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
    public float sensX = 0.18f;                 // �¿� ����
    public float sensY = 0.14f;                 // ���� ����
    public Vector2 pitchClamp = new Vector2(-30f, 60f);

    [Header("Smoothing")]
    public float rotationResponsiveness = 20f;  // Ŭ���� �ﰢ�� (12~26 ����)
    public float followLerp = 18f;

    [Header("Cursor/UI")]
    [Tooltip("Alt�� ������ ���� Ŀ�� ǥ�� + ī�޶� ȸ�� ����")]
    public bool holdAltToShowCursor = true;

    private float targetYaw;
    private float targetPitch;
    private float yaw;
    private float pitch;

    private Vector2 lookInput;   // �̺�Ʈ ���(������)

    // --- Invoke Unity Events ���� ---
    public void OnLook(InputAction.CallbackContext ctx) { lookInput = ctx.ReadValue<Vector2>(); }
    public void OnZoom(InputAction.CallbackContext ctx) { ApplyZoom(ctx.ReadValue<float>()); }
    // --- Send Messages ���� ---
    void OnLook(InputValue v) { lookInput = v.Get<Vector2>(); }
    void OnZoom(InputValue v) { ApplyZoom(v.Get<float>()); }

    void ApplyZoom(float scrollY)
    {
        distance = Mathf.Clamp(distance - scrollY * 0.1f * (maxDistance - minDistance), minDistance, maxDistance);
    }

    void LateUpdate()
    {
        if (!target) return;

        // 0) Alt ����: Ŀ��/ȸ�� ����
        bool altHeld = holdAltToShowCursor &&
                       Keyboard.current != null &&
                       (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed);

        if (altHeld)
        {
            // UI ���: Ŀ�� ���̰� + ȸ�� �Է� ����
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // �÷��� ���: Ŀ�� ���� + ���
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // 1) �Է� Ȯ��: �̺�Ʈ �켱, ������ ����(�ȼ� ����)
        Vector2 delta = Vector2.zero;
        if (!altHeld) // Alt ������ ȸ�� �Է� ����
        {
            delta = lookInput;
            if (delta == Vector2.zero && Mouse.current != null)
                delta = Mouse.current.delta.ReadValue();
        }

        // 2) ��ǥ ���� ���� (�ȼ��氢�� ��ȯ, ������ ����: Time.deltaTime ������ ����)
        if (delta != Vector2.zero)
        {
            targetYaw += delta.x * sensX;
            targetPitch -= delta.y * sensY;
            targetPitch = Mathf.Clamp(targetPitch, pitchClamp.x, pitchClamp.y);
        }

        // 3) ���� ������(�������� ����)
        float t = 1f - Mathf.Exp(-rotationResponsiveness * Time.unscaledDeltaTime);
        yaw = Mathf.LerpAngle(yaw, targetYaw, t);
        pitch = Mathf.LerpAngle(pitch, targetPitch, t);

        // 4) ī�޶� ��ġ/ȸ�� ����
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + focusOffset;
        Vector3 desiredPos = pivot + rot * new Vector3(0f, 0f, -distance);

        transform.position = Vector3.Lerp(transform.position, desiredPos, followLerp * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);

        // 5) ������ ���� ����
        lookInput = Vector2.zero;
    }
}
