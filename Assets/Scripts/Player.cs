using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float deadZone = 0.05f;

    [Header("Dash")]
    [Tooltip("돌진 시작 속도(한 순간의 최대 속도)")]
    [SerializeField] private float dashSpeed = 10f;
    [Tooltip("돌진 지속 시간(초)")]
    [SerializeField] private float dashDuration = 0.18f;
    [Tooltip("돌진 쿨타임(초)")]
    [SerializeField] private float dashCooldown = 0.6f;
    [Tooltip("돌진 속도 감쇠 곡선(없으면 선형 1→0)")]
    [SerializeField] private AnimationCurve dashCurve = null;

    [Header("Animator Params / States")]
    [SerializeField] private string speedParam = "Speed";          // BlendTree 제어 float
    [SerializeField] private float speedDampTime = 0.10f;             // SetFloat 댐핑 시간
    [SerializeField] private string dashBoolParam = "isDashing";       // 대쉬 잠금 Bool
    [SerializeField] private string dashStateName = "Male Action Pose";// 대쉬 상태(정지 포즈)
    [SerializeField] private string locomotionState = "Locomotion";      // BlendTree가 있는 상태 이름
    [SerializeField] private float locomotionFade = 0.08f;             // 대쉬→로코모션 페이드

    // 입력 크기를 0~1로 노멀라이즈하는 기준 (WASD 1.0, 패드 스틱 1.0 가정)
    [SerializeField] private float inputToSpeedScale = 1.0f;              // 1이면 magnitude(0~1)를 그대로 Speed에 씀

    private bool isDashing = false;         // 코드 레벨 대쉬 상태(이동/회전 차단용)
    private float lastDashEndTime = -999f;
    private Vector3 dashDir;

    private Vector2 moveInput;              // (x:좌우, y:전후)
    private Animator anim;
    private Transform cam;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (Camera.main) cam = Camera.main.transform;
        if (dashCurve == null) dashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    }

    void Update()
    {
        // --- 공통: Speed 파라미터는 '항상' 댐핑으로 갱신 (대쉬 중에도)
        float targetSpeed = Mathf.Clamp01(moveInput.magnitude * inputToSpeedScale);
        if (anim) anim.SetFloat(speedParam, targetSpeed, speedDampTime, Time.deltaTime);

        // 대쉬 중이면 이동/회전은 코드로 막고, 애니메이션만 Speed가 천천히 맞춰진다
        if (isDashing) return;

        if (targetSpeed < deadZone)
        {
            // 입력 거의 없음: 이동/회전 없음 (Speed는 위에서 0으로 댐핑됨)
            return;
        }

        // 카메라 기준 이동 방향
        Vector3 camFwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        Vector3 moveDir = (camFwd * moveInput.y + camRight * moveInput.x).normalized;

        // 회전
        Quaternion targetRot = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);

        // 로컬 전진 이동
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);
    }

    // ===== Input System (PlayerInput: Behavior = Send Messages) =====
    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnDash(InputValue value)
    {
        if (!value.isPressed) return;
        if (Time.time < lastDashEndTime + dashCooldown) return;
        if (isDashing) return;

        // 돌진 방향: 입력 있으면 입력 방향, 없으면 현재 전방
        Vector3 camFwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        Vector3 fromInput = (camFwd * moveInput.y + camRight * moveInput.x);
        dashDir = (fromInput.sqrMagnitude > 0.0001f) ? fromInput.normalized : transform.forward;

        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;

        // 1) 애니메이터: 대쉬 잠금 + 대쉬 상태로 즉시 진입
        if (anim)
        {
            anim.SetBool(dashBoolParam, true);
            anim.CrossFadeInFixedTime(dashStateName, 0.06f, 0, 0f);
        }

        // 2) 방향 정렬
        if (dashDir.sqrMagnitude > 0f)
            transform.rotation = Quaternion.LookRotation(dashDir);

        // 3) 대쉬 이동(코드 이동)
        float t = 0f;
        while (t < dashDuration)
        {
            float norm = dashDuration > 0f ? t / dashDuration : 1f;        // 0→1
            float speedScale = dashCurve != null ? dashCurve.Evaluate(norm) : (1f - norm);
            float frameSpeed = dashSpeed * Mathf.Max(0f, speedScale);

            transform.Translate(dashDir * frameSpeed * Time.deltaTime, Space.World);

            t += Time.deltaTime;
            yield return null;
        }

        // 4) 대쉬 잠금 해제
        if (anim) anim.SetBool(dashBoolParam, false);

        // 5) 대쉬 종료 즉시 Locomotion(BlendTree)로 부드럽게 복귀
        //    Speed는 이미 Update에서 댐핑으로 현재 입력에 맞춰지고 있으므로 자연스럽게 이어짐
        if (anim) anim.CrossFadeInFixedTime(locomotionState, locomotionFade, 0, 0f);

        isDashing = false;
        lastDashEndTime = Time.time;
    }
}
