using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] EnemyChase enemyPrefab;
    [SerializeField] Transform player;
    [Header("Spawn rate")]
    [SerializeField] float startInterval = 1.5f;
    [SerializeField] float minInterval = 0.25f;
    [SerializeField] float rampSeconds = 120f;
    [Header("Limits")]
    [SerializeField] private int maxEnemies = 150;
    [Header("Placement")]
    [SerializeField] float spawnPadding = 1.5f;
    [SerializeField] float extraDepth = 4f;
    [Header("Surround events (0 = off)")]
    [SerializeField] float surroundEvery = 30f;
    [SerializeField] int surroundCount = 14;
    Camera cam;
    float timer;
    float elapsed;
    private float surroundTimer;

    void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (player == null || enemyPrefab == null) return;

        elapsed += Time.deltaTime;
        if (surroundEvery > 0f)
        {
            surroundTimer += Time.deltaTime;
            if (surroundTimer >= surroundEvery)
            {
                surroundTimer = 0f;
                SpawnRing();
            }
        }


        timer -= Time.deltaTime;
        if (timer > 0f) return;
        float t = Mathf.Clamp01(elapsed / rampSeconds);
        timer = Mathf.Lerp(startInterval, minInterval, t);

        if (EnemyChase.Active.Count >= maxEnemies) return;


        SpawnOne();
    }
    void SpawnOne()
    {
        float halfH = cam.orthographicSize + spawnPadding;
        float halfW = cam.orthographicSize * cam.aspect + spawnPadding;
        float w = halfW * 2f;
        float h = halfH * 2f;


        float p = Random.value * 2f * (w + h);
        Vector2 local;
        Vector2 normal;
        if (p < w)
        {
            local = new Vector2(-halfW + p, halfH);
            normal = Vector2.up;
        }
        else if ((p -= w) < h)
        {
            local = new Vector2(halfW, halfH - p);
            normal = Vector2.right;
        }
        else if ((p -= h) < w)
        {
            local = new Vector2(halfW - p, -halfH);
            normal = Vector2.down;
        }
        else
        {
            p -= w;
            local = new Vector2(-halfW, -halfH + p);
            normal = Vector2.left;
        }

        Vector2 center = cam.transform.position;
        Vector2 pos = center + local + normal * Random.Range(0f, extraDepth);
        Instantiate(enemyPrefab, pos, Quaternion.identity);
    }
    void SpawnRing()
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float radius = Mathf.Sqrt(halfW * halfW + halfH * halfH) + spawnPadding + 1f;
        float offset = Random.value * Mathf.PI * 2f;
        for (int i = 0; i < surroundCount; i++)
        {
            if (EnemyChase.Active.Count >= maxEnemies) break;
            float angle = offset + i * Mathf.PI * 2f / surroundCount;
            Vector2 pos = (Vector2)player.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Instantiate(enemyPrefab, pos, Quaternion.identity);
        }
    }
}