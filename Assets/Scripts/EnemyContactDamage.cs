using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [SerializeField] private float damage = 1f;
    [SerializeField] private float cooldown = 0.5f;
    private float nextHitTime;
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (Time.time < nextHitTime) return;
        if (collision.gameObject.TryGetComponent(out PlayerController _) &&
            collision.gameObject.TryGetComponent(out Health health))
        {
            health.TakeDamage(damage);
            nextHitTime = Time.time + cooldown;
        }
    }
}