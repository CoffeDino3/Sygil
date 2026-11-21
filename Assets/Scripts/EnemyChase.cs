using System.Collections.Generic;
using UnityEngine;

public class EnemyChase : MonoBehaviour
{
    public static readonly HashSet<EnemyChase> Active = new HashSet<EnemyChase>();
    [SerializeField] private float moveSpeed = 2.5f;
    private Rigidbody2D rb;
    private Transform target;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) target = player.transform;
    }
    private void OnEnable()
    {
        Active.Add(this);
    }
    private void OnDisable()
    {
        Active.Remove(this);
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