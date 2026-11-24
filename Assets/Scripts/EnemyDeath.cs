using UnityEngine;
public class EnemyDeath : MonoBehaviour
{
    [SerializeField] private GameObject gemPrefab;
    Health health;
    private void Awake()
    {
        health = GetComponent<Health>();
        health.OnDied += HandleDeath;
    }

    private void OnDestroy()
    {
        health.OnDied -= HandleDeath;
    }

    void HandleDeath()
    {
        EnemyEvents.RaiseDied(transform.position);
        if (gemPrefab != null)
            Instantiate(gemPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}