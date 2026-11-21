using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyChase enemyPrefab;
    [SerializeField] private Transform player;
    [Header("Spawn rate")]
    [SerializeField] private float startInterval = 1.5f;
    [SerializeField] private float minInterval = 0.25f;
    [SerializeField] private float rampSeconds = 120f;
    [Header("Limits")]
    [SerializeField] private int maxEnemies = 150;
    [SerializeField] private float spawnPadding = 2f;

    private Camera cam;
    private float timer;
    private float elapsed;
    private void Awake()
    {
        cam = Camera.main;
    }
    private void Update()
    {
        if (player == null || enemyPrefab == null) return;
        elapsed += Time.deltaTime;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        float t = Mathf.Clamp01(elapsed / rampSeconds);
        timer = Mathf.Lerp(startInterval, minInterval, t);
        if (EnemyChase.Active.Count >= maxEnemies) return;
        Spawn();
    }

    private void Spawn()
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        float radius = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) + spawnPadding;
        Vector2 dir = Random.insideUnitCircle.normalized;
        Vector2 pos = (Vector2)player.position + dir * radius;
        Instantiate(enemyPrefab, pos, Quaternion.identity);
    }
}