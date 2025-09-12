using UnityEngine;

public class BoneFollowerPro : MonoBehaviour
{
    [Tooltip("���� ��(Transform)")]
    public Transform targetBone;

    [Tooltip("���� ���� �������� �ݶ��̴��� ��ġ ������")]
    public Vector3 localPositionOffset;

    [Tooltip("���� ���� �������� �ݶ��̴��� ȸ�� ������")]
    public Quaternion localRotationOffset = Quaternion.identity;

    [Tooltip("���� �� ���� �ݶ��̴��� ���� Pose�� �������� �ڵ����� ������ ���")]
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

    // ��ƿ: �����Ϳ��� ���� ȣ���
    public void RecalculateOffsetsFromCurrent()
    {
        if (!targetBone) return;
        localPositionOffset = targetBone.InverseTransformPoint(transform.position);
        localRotationOffset = Quaternion.Inverse(targetBone.rotation) * transform.rotation;
    }
}
