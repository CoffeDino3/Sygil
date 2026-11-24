using System.Collections.Generic;
using UnityEngine;

public class EnemyChase : MonoBehaviour
{
    public static readonly HashSet<EnemyChase> Active = new HashSet<EnemyChase>();
    [SerializeField] float moveSpeed = 2.5f;

    [Header("Crowd spacing")]
    [SerializeField] float separationRadius = 0.7f;
    [SerializeField] float separationStrength = 1.2f;

    [Header("Combat")]
    [SerializeField] float hitRadius = 0.25f;

    Rigidbody2D rb;
    Transform target;
    float slowFactor = 1f;
    float slowTimer;
    Vector2 impulse;
    public Health Health { get; private set; }
    public Vector2 Position => rb.position;
    public float HitRadius => hitRadius;
    public void ApplySlow(float amount, float duration)
    {
        float factor = 1f - Mathf.Clamp01(amount);
        if (slowTimer <= 0f || factor < slowFactor) slowFactor = factor;
        slowTimer = Mathf.Max(slowTimer, duration);
    }
    public void Knockback(Vector2 velocity)
    {
        impulse = Vector2.ClampMagnitude(impulse + velocity, 16f);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Health = GetComponent<Health>();
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) target = player.transform;
    }

    void OnEnable() { Active.Add(this); }
    void OnDisable() { Active.Remove(this); }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (slowTimer > 0f)
        {
            slowTimer -= dt;
            if (slowTimer <= 0f) slowFactor = 1f;
        }
        Vector2 goal = Vector2.zero;
        bool hasGoal = false;
        if (ZoneSystem.DecoyPositions.Count > 0 && TryNearestDecoy(out goal))
        {
            hasGoal = true;
        }
        else if (target != null)
        {
            goal = target.position;
            hasGoal = true;
        }

        Vector2 dir = Vector2.zero;
        if (hasGoal)
        {
            Vector2 toGoal = goal - rb.position;
            if (toGoal.sqrMagnitude > 0.0001f) dir = toGoal.normalized;
        }
        Vector2 push = Vector2.zero;
        float radiusSqr = separationRadius * separationRadius;
        foreach (EnemyChase other in Active)
        {
            if (other == this) continue;
            Vector2 away = rb.position - other.rb.position;
            float sqr = away.sqrMagnitude;


            if (sqr < 0.0001f)
            {
                push += Random.insideUnitCircle;
                continue;
            }
            if (sqr > radiusSqr) continue;
            float dist = Mathf.Sqrt(sqr);
            push += (away / dist) * (1f - dist / separationRadius);
        }

        Vector2 move = dir + push * separationStrength;
        if (move.sqrMagnitude > 1f) move.Normalize();
        rb.linearVelocity = move * (moveSpeed * slowFactor) + impulse;
        impulse *= Mathf.Exp(-8f * dt);
    }

    bool TryNearestDecoy(out Vector2 position)
    {
        position = Vector2.zero;
        float bestSqr = 7f * 7f;
        bool found = false;
        List<Vector2> decoys = ZoneSystem.DecoyPositions;
        for (int i = 0; i < decoys.Count; i++)
        {
            float sqr = (decoys[i] - rb.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                position = decoys[i];
                found = true;
            }
        }
        return found;
    }
}