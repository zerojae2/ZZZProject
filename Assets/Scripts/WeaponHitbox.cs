using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    public int damage = 10;
    private Collider col;

    void Awake() { col = GetComponent<Collider>(); col.enabled = false; }

    // �ִϸ��̼� �̺�Ʈ���� ȣ��
    public void EnableHitbox() { col.enabled = true; }
    public void DisableHitbox() { col.enabled = false; }

    void OnTriggerEnter(Collider other)
    {
        if (!col.enabled) return;
        var d = other.GetComponent<IDamageable>(); // �������̽� ����
        if (d != null) d.TakeDamage(damage);
    }
}

public interface IDamageable { void TakeDamage(int amount); }
