using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Rigidbody2D rb;
    private float damage;
    private int pierceLeft;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 direction, float speed, float damage, float lifetime, int pierce)
    {
        this.damage = damage;
        pierceLeft = pierce;
        rb.linearVelocity = direction * speed;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out EnemyChase _)) return;

        if (other.TryGetComponent(out Health health))
            health.TakeDamage(damage);
        if (pierceLeft <= 0)
            Destroy(gameObject);
        else
            pierceLeft--;
    }
}