using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    public int damage = 10;
    private Collider col;

    void Awake() { col = GetComponent<Collider>(); col.enabled = false; }

    // 애니메이션 이벤트에서 호출
    public void EnableHitbox() { col.enabled = true; }
    public void DisableHitbox() { col.enabled = false; }

    void OnTriggerEnter(Collider other)
    {
        if (!col.enabled) return;
        var d = other.GetComponent<IDamageable>(); // 인터페이스 권장
        if (d != null) d.TakeDamage(damage);
    }
}

public interface IDamageable { void TakeDamage(int amount); }
