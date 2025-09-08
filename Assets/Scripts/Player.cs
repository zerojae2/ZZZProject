using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float deadZone = 0.05f;

    private Vector2 moveInput;
    private Animator anim;
    private Transform cam;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (Camera.main) cam = Camera.main.transform;
    }

    void Update()
    {
        if (moveInput.sqrMagnitude < deadZone * deadZone)
        {
            if (anim) anim.SetBool("isWalking", false);
            return;
        }

        // 카메라 기준 이동 (MMORPG 스타일)
        Vector3 camFwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;

        Vector3 moveDir = camFwd * moveInput.y + camRight * moveInput.x;
        moveDir.Normalize();

        // 회전
        Quaternion targetRot = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);

        // 이동
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);

        if (anim) anim.SetBool("isWalking", true);
    }

    // PlayerInput → Behavior: Send Messages
    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        // Debug.Log($"[Player] Input: {moveInput}");
    }
}
