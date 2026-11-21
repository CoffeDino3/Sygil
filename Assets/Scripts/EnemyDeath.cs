using UnityEngine;

public class EnemyDeath : MonoBehaviour
{
    private Health health;
    private void Awake()
    {
        health = GetComponent<Health>();
        health.OnDied += HandleDeath;
    }
    private void OnDestroy()
    {
        health.OnDied -= HandleDeath;
    }
    [SerializeField] private GameObject gemPrefab;
    private void HandleDeath()
    {
        if (gemPrefab != null)
            Instantiate(gemPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

}