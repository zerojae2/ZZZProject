using System.Collections.Generic;
using UnityEngine;

public class AutoBindCollidersToBones : MonoBehaviour
{
    [Header("Roots")]
    [Tooltip("스켈레톤(Armature) 루트. 예: mixamorig:Hips 상위 본")]
    public Transform skeletonRoot;

    [Tooltip("대상 콜라이더들이 들어있는 루트(비우면 이 컴포넌트 오브젝트 기준 하위 전체 검색)")]
    public Transform collidersRoot;

    [Header("Filters")]
    [Tooltip("이 레이어의 콜라이더만 처리(비우면 전체)")]
    public LayerMask colliderLayerMask = ~0;

    [Tooltip("이름에 포함되면 제외할 콜라이더 키워드(루트 캡슐 등)")]
    public string[] excludeNameContains = new string[] { "CapsuleRoot", "MainCapsule", "BodyCollider" };

    [Tooltip("Trigger만 대상으로 할지 여부(공격/피격용). false면 모든 Collider 대상")]
    public bool onlyTriggers = false;

    [Header("Binding")]
    [Tooltip("오프셋을 현재 Pose 기준으로 자동 계산하여 저장")]
    public bool computeOffsetsFromCurrentPose = true;

    [Tooltip("이미 BoneFollower가 붙어 있으면 덮어쓸지 여부")]
    public bool overwriteExisting = true;

    [Header("Debug")]
    public bool logSummary = true;
    public Color gizmoLineColor = new Color(0f, 1f, 0.7f, 0.8f);

    List<Transform> _bones;

    void Reset()
    {
        // 기본값: 이 컴포넌트 오브젝트 하위에서 콜라이더 검색
        collidersRoot = transform;
    }

    void Awake()
    {
        if (!skeletonRoot)
        {
            Debug.LogError("[AutoBindCollidersToBones] skeletonRoot가 비어있습니다.");
            enabled = false;
            return;
        }

        if (!collidersRoot) collidersRoot = transform;

        // 모든 본 수집
        _bones = new List<Transform>();
        CollectBonesRecursive(skeletonRoot, _bones);

        // 콜라이더 검색 + 바인딩
        var cols = collidersRoot.GetComponentsInChildren<Collider>(includeInactive: true);
        int bound = 0, skipped = 0;

        foreach (var col in cols)
        {
            if (!IsColliderEligible(col)) { skipped++; continue; }

            // 가장 가까운 본 찾기 (Collider의 중심 기준)
            Vector3 center = GetColliderWorldCenter(col);
            Transform nearestBone = FindNearestBone(center);

            if (!nearestBone)
            {
                Debug.LogWarning($"[AutoBind] 가까운 본을 찾지 못했습니다: {col.name}");
                skipped++;
                continue;
            }

            // BoneFollowerPro 부착/설정
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

            // 공격/피격용 Trigger 추천 세팅(선택)
            if (onlyTriggers && !col.isTrigger)
                col.isTrigger = true;

            // 리지드바디가 있다면 히트박스 용도로는 kinematic 권장
            var rb = col.attachedRigidbody;
            if (rb) rb.isKinematic = true;

            bound++;
        }

        if (logSummary)
            Debug.Log($"[AutoBindCollidersToBones] 본 {_bones.Count}개, 콜라이더 {cols.Length}개 중 {bound}개 바인딩, {skipped}개 스킵.");
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

        // 레이어 필터
        if (((1 << col.gameObject.layer) & colliderLayerMask.value) == 0) return false;

        return true;
    }

    Vector3 GetColliderWorldCenter(Collider col)
    {
        // 다양한 콜라이더 타입 지원
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
