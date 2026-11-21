using UnityEngine;

public class SigilCaster : MonoBehaviour
{
    [SerializeField] private SigilData sigil;
    private float timer;
    private void Update()
    {
        if (sigil == null) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        Transform target = FindNearestEnemy();
        if (target == null) return;
        Fire(target);
        timer = sigil.cooldown;
    }

    private Transform FindNearestEnemy()
    {
        Transform nearest = null;
        float bestSqr = sigil.targetRange * sigil.targetRange;
        foreach (EnemyChase enemy in EnemyChase.Active)
        {
            float sqr = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearest = enemy.transform;
            }
        }
        return nearest;
    }

    private void Fire(Transform target)
    {
        Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
        Projectile p = Instantiate(sigil.projectilePrefab, transform.position, Quaternion.identity);
        p.Launch(dir, sigil.projectileSpeed, sigil.damage, sigil.lifetime, sigil.pierce);
    }
}