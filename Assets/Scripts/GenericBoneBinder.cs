using UnityEngine;

[System.Serializable]
public class ColliderNameMap
{
    public Collider collider;
    public string boneName;  // Hierarchy에서 정확한 본 이름
    public bool attachAsChild = true;
    public bool computeOffsetsFromCurrent = true;
}

public class GenericBoneBinder : MonoBehaviour
{
    public Transform skeletonRoot;
    public ColliderNameMap[] maps;

    void Start()
    {
        foreach (var m in maps)
        {
            if (m == null || !m.collider) continue;

            var bone = FindDeep(skeletonRoot, m.boneName);
            if (!bone)
            {
                Debug.LogWarning($"[GenericBoneBinder] Bone not found: {m.boneName}");
                continue;
            }

            var f = m.collider.GetComponent<BoneFollowerStrong>();
            if (!f) f = m.collider.gameObject.AddComponent<BoneFollowerStrong>();

            f.targetBone = bone;
            f.attachAsChild = m.attachAsChild;
            f.updateMode = BoneFollowerStrong.FollowMode.FixedUpdate;
            f.syncTransformsInFixed = true;

            if (m.computeOffsetsFromCurrent)
            {
                f.localPositionOffset = bone.InverseTransformPoint(m.collider.transform.position);
                f.localEulerOffset = (Quaternion.Inverse(bone.rotation) * m.collider.transform.rotation).eulerAngles;
            }
        }
    }

    Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var result = FindDeep(root.GetChild(i), name);
            if (result) return result;
        }
        return null;
    }
}
