using UnityEngine;

public class BoneFollowerStrong : MonoBehaviour
{
    public enum FollowMode { LateUpdate, FixedUpdate, OnAnimatorMove }
    [Header("Target")]
    public Transform targetBone;

    [Header("Offsets (in bone local space)")]
    public Vector3 localPositionOffset;
    public Vector3 localEulerOffset;

    [Header("Options")]
    [Tooltip("Start���� ��� ���� ��¥ �ڽ����� ���Դϴ�(���� �߰�).")]
    public bool attachAsChild = false;
    [Tooltip("���� ���� �������� �������� �ڵ� ����մϴ�.")]
    public bool computeOffsetsFromCurrentPose = true;
    [Tooltip("������Ʈ Ÿ�̹��� �����մϴ�.")]
    public FollowMode updateMode = FollowMode.LateUpdate;
    [Tooltip("FixedUpdate ����� ��, �������� Physics.SyncTransforms()�� ȣ���մϴ�.")]
    public bool syncTransformsInFixed = true;

    [Header("Smoothing (optional)")]
    [Range(0f, 1f)] public float positionSmoothing = 0f; // 0=�ﰢ, 0.2~0.5 ����
    [Range(0f, 1f)] public float rotationSmoothing = 0f; // 0=�ﰢ

    Quaternion localRotOffsetQ = Quaternion.identity;

    void Start()
    {
        if (!targetBone) { enabled = false; return; }

        if (computeOffsetsFromCurrentPose)
        {
            localPositionOffset = targetBone.InverseTransformPoint(transform.position);
            localRotOffsetQ = Quaternion.Inverse(targetBone.rotation) * transform.rotation;
            localEulerOffset = localRotOffsetQ.eulerAngles;
        }
        else
        {
            localRotOffsetQ = Quaternion.Euler(localEulerOffset);
        }

        if (attachAsChild)
        {
            // �ڽ�ȭ�ϸ鼭 ���� �������� ����
            Vector3 worldPos = targetBone.TransformPoint(localPositionOffset);
            Quaternion worldRot = targetBone.rotation * Quaternion.Euler(localEulerOffset);
            transform.SetParent(targetBone, worldPositionStays: false);
            transform.SetLocalPositionAndRotation(localPositionOffset, Quaternion.Euler(localEulerOffset));
            // Ȥ�� ���� ������ ���ϸ� �� �� �� ���:
            // transform.SetParent(targetBone, true);
            // transform.SetPositionAndRotation(worldPos, worldRot);
        }
    }

    void LateUpdate()
    {
        if (updateMode != FollowMode.LateUpdate || attachAsChild || !targetBone) return;
        ApplyPose();
    }

    void FixedUpdate()
    {
        if (updateMode != FollowMode.FixedUpdate || attachAsChild || !targetBone) return;
        ApplyPose();
        if (syncTransformsInFixed) Physics.SyncTransforms();
    }

    // Animator�� �ִ� ���� ������Ʈ�� �ٿ�, Animator.OnAnimatorMove �� ȣ���ϰ� ���� ��
    void OnAnimatorMove()
    {
        if (updateMode != FollowMode.OnAnimatorMove || attachAsChild || !targetBone) return;
        ApplyPose();
    }

    void ApplyPose()
    {
        // ȸ��/��ġ�� �� ����
        Quaternion worldRot = targetBone.rotation * Quaternion.Euler(localEulerOffset);
        Vector3 worldPos = targetBone.TransformPoint(localPositionOffset);
        transform.SetPositionAndRotation(worldPos, worldRot);

        Quaternion targetRot = targetBone.rotation * Quaternion.Euler(localEulerOffset);
        Vector3 targetPos = targetBone.TransformPoint(localPositionOffset);

        if (positionSmoothing > 0f)
            transform.position = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Pow(1f - positionSmoothing, Time.deltaTime * 60f));
        else
            transform.position = targetPos;

        if (rotationSmoothing > 0f)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Pow(1f - rotationSmoothing, Time.deltaTime * 60f));
        else
            transform.rotation = targetRot;
    }

    // ����: ������ ����
    public void RecalculateOffsetsFromCurrent()
    {
        if (!targetBone) return;
        localPositionOffset = targetBone.InverseTransformPoint(transform.position);
        localEulerOffset = (Quaternion.Inverse(targetBone.rotation) * transform.rotation).eulerAngles;
    }
}
