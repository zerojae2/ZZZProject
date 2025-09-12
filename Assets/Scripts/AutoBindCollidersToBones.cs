using System.Collections.Generic;
using UnityEngine;

public class AutoBindCollidersToBones : MonoBehaviour
{
    [Header("Roots")]
    [Tooltip("���̷���(Armature) ��Ʈ. ��: mixamorig:Hips ���� ��")]
    public Transform skeletonRoot;

    [Tooltip("��� �ݶ��̴����� ����ִ� ��Ʈ(���� �� ������Ʈ ������Ʈ ���� ���� ��ü �˻�)")]
    public Transform collidersRoot;

    [Header("Filters")]
    [Tooltip("�� ���̾��� �ݶ��̴��� ó��(���� ��ü)")]
    public LayerMask colliderLayerMask = ~0;

    [Tooltip("�̸��� ���ԵǸ� ������ �ݶ��̴� Ű����(��Ʈ ĸ�� ��)")]
    public string[] excludeNameContains = new string[] { "CapsuleRoot", "MainCapsule", "BodyCollider" };

    [Tooltip("Trigger�� ������� ���� ����(����/�ǰݿ�). false�� ��� Collider ���")]
    public bool onlyTriggers = false;

    [Header("Binding")]
    [Tooltip("�������� ���� Pose �������� �ڵ� ����Ͽ� ����")]
    public bool computeOffsetsFromCurrentPose = true;

    [Tooltip("�̹� BoneFollower�� �پ� ������ ����� ����")]
    public bool overwriteExisting = true;

    [Header("Debug")]
    public bool logSummary = true;
    public Color gizmoLineColor = new Color(0f, 1f, 0.7f, 0.8f);

    List<Transform> _bones;

    void Reset()
    {
        // �⺻��: �� ������Ʈ ������Ʈ �������� �ݶ��̴� �˻�
        collidersRoot = transform;
    }

    void Awake()
    {
        if (!skeletonRoot)
        {
            Debug.LogError("[AutoBindCollidersToBones] skeletonRoot�� ����ֽ��ϴ�.");
            enabled = false;
            return;
        }

        if (!collidersRoot) collidersRoot = transform;

        // ��� �� ����
        _bones = new List<Transform>();
        CollectBonesRecursive(skeletonRoot, _bones);

        // �ݶ��̴� �˻� + ���ε�
        var cols = collidersRoot.GetComponentsInChildren<Collider>(includeInactive: true);
        int bound = 0, skipped = 0;

        foreach (var col in cols)
        {
            if (!IsColliderEligible(col)) { skipped++; continue; }

            // ���� ����� �� ã�� (Collider�� �߽� ����)
            Vector3 center = GetColliderWorldCenter(col);
            Transform nearestBone = FindNearestBone(center);

            if (!nearestBone)
            {
                Debug.LogWarning($"[AutoBind] ����� ���� ã�� ���߽��ϴ�: {col.name}");
                skipped++;
                continue;
            }

            // BoneFollowerPro ����/����
            var follower = col.GetComponent<BoneFollowerPro>();
            if (follower && !overwriteExisting) { bound++; continue; }
            if (!follower) follower = col.gameObject.AddComponent<BoneFollowerPro>();

            follower.targetBone = nearestBone;

            if (computeOffsetsFromCurrentPose)
            {
                follower.localPositionOffset = nearestBone.InverseTransformPoint(col.transform.position);
                follower.localRotationOffset = Quaternion.Inverse(nearestBone.rotation) * col.transform.rotation;
                follower.initializeOffsetsOnStart = false;
            }

            // ����/�ǰݿ� Trigger ��õ ����(����)
            if (onlyTriggers && !col.isTrigger)
                col.isTrigger = true;

            // ������ٵ� �ִٸ� ��Ʈ�ڽ� �뵵�δ� kinematic ����
            var rb = col.attachedRigidbody;
            if (rb) rb.isKinematic = true;

            bound++;
        }

        if (logSummary)
            Debug.Log($"[AutoBindCollidersToBones] �� {_bones.Count}��, �ݶ��̴� {cols.Length}�� �� {bound}�� ���ε�, {skipped}�� ��ŵ.");
    }

    void CollectBonesRecursive(Transform t, List<Transform> list)
    {
        list.Add(t);
        for (int i = 0; i < t.childCount; i++)
            CollectBonesRecursive(t.GetChild(i), list);
    }

    bool IsColliderEligible(Collider col)
    {
        if (excludeNameContains != null)
        {
            foreach (var key in excludeNameContains)
            {
                if (!string.IsNullOrEmpty(key) && col.name.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
            }
        }

        if (onlyTriggers && !col.isTrigger) return false;

        // ���̾� ����
        if (((1 << col.gameObject.layer) & colliderLayerMask.value) == 0) return false;

        return true;
    }

    Vector3 GetColliderWorldCenter(Collider col)
    {
        // �پ��� �ݶ��̴� Ÿ�� ����
        switch (col)
        {
            case BoxCollider b: return b.transform.TransformPoint(b.center);
            case CapsuleCollider c: return c.transform.TransformPoint(c.center);
            case SphereCollider s: return s.transform.TransformPoint(s.center);
            default: return col.bounds.center;
        }
    }

    Transform FindNearestBone(Vector3 worldPoint)
    {
        Transform best = null;
        float bestSqr = float.MaxValue;
        foreach (var b in _bones)
        {
            float d = (b.position - worldPoint).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = b; }
        }
        return best;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (_bones == null) return;
        Gizmos.color = gizmoLineColor;

        var cols = collidersRoot ? collidersRoot.GetComponentsInChildren<Collider>(true)
                                 : GetComponentsInChildren<Collider>(true);

        foreach (var col in cols)
        {
            var fol = col.GetComponent<BoneFollowerPro>();
            if (fol && fol.targetBone)
            {
                Gizmos.DrawLine(GetColliderWorldCenter(col), fol.targetBone.position);
            }
        }
    }
#endif
}
