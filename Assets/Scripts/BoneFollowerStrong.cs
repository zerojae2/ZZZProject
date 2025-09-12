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
    [Tooltip("Start에서 대상 뼈의 진짜 자식으로 붙입니다(가장 견고).")]
    public bool attachAsChild = false;
    [Tooltip("현재 포즈 기준으로 오프셋을 자동 계산합니다.")]
    public bool computeOffsetsFromCurrentPose = true;
    [Tooltip("업데이트 타이밍을 선택합니다.")]
    public FollowMode updateMode = FollowMode.LateUpdate;
    [Tooltip("FixedUpdate 모드일 때, 마지막에 Physics.SyncTransforms()를 호출합니다.")]
    public bool syncTransformsInFixed = true;

    [Header("Smoothing (optional)")]
    [Range(0f, 1f)] public float positionSmoothing = 0f; // 0=즉각, 0.2~0.5 권장
    [Range(0f, 1f)] public float rotationSmoothing = 0f; // 0=즉각

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
            // 자식화하면서 현재 오프셋을 보존
            Vector3 worldPos = targetBone.TransformPoint(localPositionOffset);
            Quaternion worldRot = targetBone.rotation * Quaternion.Euler(localEulerOffset);
            transform.SetParent(targetBone, worldPositionStays: false);
            transform.SetLocalPositionAndRotation(localPositionOffset, Quaternion.Euler(localEulerOffset));
            // 혹시 월드 정합을 원하면 위 두 줄 대신:
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

    // Animator가 있는 같은 오브젝트에 붙여, Animator.OnAnimatorMove 후 호출하고 싶을 때
    void OnAnimatorMove()
    {
        if (updateMode != FollowMode.OnAnimatorMove || attachAsChild || !targetBone) return;
        ApplyPose();
    }

    void ApplyPose()
    {
        // 회전/위치를 한 번에
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

    // 편의: 오프셋 재계산
    public void RecalculateOffsetsFromCurrent()
    {
        if (!targetBone) return;
        localPositionOffset = targetBone.InverseTransformPoint(transform.position);
        localEulerOffset = (Quaternion.Inverse(targetBone.rotation) * transform.rotation).eulerAngles;
    }
}
