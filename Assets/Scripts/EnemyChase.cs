using UnityEngine;

public class EnemyChase : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.5f;

    private Rigidbody2D rb;
    private Transform target;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) target = player.transform;
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = ((Vector2)target.position - rb.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }
}