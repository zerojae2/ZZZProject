using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 focusOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Orbit Distance")]
    public float distance = 5f;
    public float minDistance = 2f;
    public float maxDistance = 8f;

    [Header("Sensitivity (pixels -> degrees)")]
    public float sensX = 0.18f;                 // 좌우 감도
    public float sensY = 0.14f;                 // 상하 감도
    public Vector2 pitchClamp = new Vector2(-30f, 60f);

    [Header("Smoothing")]
    public float rotationResponsiveness = 20f;  // 클수록 즉각적 (12~26 권장)
    public float followLerp = 18f;

    [Header("Cursor/UI")]
    [Tooltip("Alt를 누르는 동안 커서 표시 + 카메라 회전 정지")]
    public bool holdAltToShowCursor = true;

    private float targetYaw;
    private float targetPitch;
    private float yaw;
    private float pitch;

    private Vector2 lookInput;   // 이벤트 경로(선택적)

    // --- Invoke Unity Events 지원 ---
    public void OnLook(InputAction.CallbackContext ctx) { lookInput = ctx.ReadValue<Vector2>(); }
    public void OnZoom(InputAction.CallbackContext ctx) { ApplyZoom(ctx.ReadValue<float>()); }
    // --- Send Messages 지원 ---
    void OnLook(InputValue v) { lookInput = v.Get<Vector2>(); }
    void OnZoom(InputValue v) { ApplyZoom(v.Get<float>()); }

    void ApplyZoom(float scrollY)
    {
        distance = Mathf.Clamp(distance - scrollY * 0.1f * (maxDistance - minDistance), minDistance, maxDistance);
    }

    void LateUpdate()
    {
        if (!target) return;

        // 0) Alt 상태: 커서/회전 제어
        bool altHeld = holdAltToShowCursor &&
                       Keyboard.current != null &&
                       (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed);

        if (altHeld)
        {
            // UI 모드: 커서 보이게 + 회전 입력 차단
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // 플레이 모드: 커서 숨김 + 잠금
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // 1) 입력 확보: 이벤트 우선, 없으면 폴링(픽셀 단위)
        Vector2 delta = Vector2.zero;
        if (!altHeld) // Alt 동안은 회전 입력 차단
        {
            delta = lookInput;
            if (delta == Vector2.zero && Mouse.current != null)
                delta = Mouse.current.delta.ReadValue();
        }

        // 2) 목표 각도 갱신 (픽셀→각도 변환, 저감도 방지: Time.deltaTime 곱하지 않음)
        if (delta != Vector2.zero)
        {
            targetYaw += delta.x * sensX;
            targetPitch -= delta.y * sensY;
            targetPitch = Mathf.Clamp(targetPitch, pitchClamp.x, pitchClamp.y);
        }

        // 3) 지수 스무딩(프레임율 독립)
        float t = 1f - Mathf.Exp(-rotationResponsiveness * Time.unscaledDeltaTime);
        yaw = Mathf.LerpAngle(yaw, targetYaw, t);
        pitch = Mathf.LerpAngle(pitch, targetPitch, t);

        // 4) 카메라 위치/회전 적용
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + focusOffset;
        Vector3 desiredPos = pivot + rot * new Vector3(0f, 0f, -distance);

        transform.position = Vector3.Lerp(transform.position, desiredPos, followLerp * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);

        // 5) 프레임 누적 방지
        lookInput = Vector2.zero;
    }
}
