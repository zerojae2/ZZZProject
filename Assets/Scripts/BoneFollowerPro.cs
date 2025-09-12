using UnityEngine;

public class BoneFollowerPro : MonoBehaviour
{
    [Tooltip("따라갈 뼈(Transform)")]
    public Transform targetBone;

    [Tooltip("뼈의 로컬 공간에서 콜라이더의 위치 오프셋")]
    public Vector3 localPositionOffset;

    [Tooltip("뼈의 로컬 공간에서 콜라이더의 회전 오프셋")]
    public Quaternion localRotationOffset = Quaternion.identity;

    [Tooltip("시작 시 현재 콜라이더의 월드 Pose를 기준으로 자동으로 오프셋 계산")]
    public bool initializeOffsetsOnStart = false;

    void Start()
    {
        if (initializeOffsetsOnStart && targetBone)
        {
            localPositionOffset = targetBone.InverseTransformPoint(transform.position);
            localRotationOffset = Quaternion.Inverse(targetBone.rotation) * transform.rotation;
        }
    }

    void LateUpdate()
    {
        if (!targetBone) return;
        transform.position = targetBone.TransformPoint(localPositionOffset);
        transform.rotation = targetBone.rotation * localRotationOffset;
    }

    // 유틸: 에디터에서 수동 호출용
    public void RecalculateOffsetsFromCurrent()
    {
        if (!targetBone) return;
        localPositionOffset = targetBone.InverseTransformPoint(transform.position);
        localRotationOffset = Quaternion.Inverse(targetBone.rotation) * transform.rotation;
    }
}
