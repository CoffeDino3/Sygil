using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(-200)]
public class SigilVfxSystem : MonoBehaviour
{
    static SigilVfxSystem instance;
    public static SigilVfxSystem Instance
    {
        get
        {
            if (instance == null) instance = new GameObject("Sigil VFX").AddComponent<SigilVfxSystem>();
            return instance;
        }
    }
    [SerializeField] Material glowMaterial;
    [SerializeField] int maxElements = 256;
    [SerializeField] int prewarm = 96;
    [SerializeField] float trailInterval = 0.065f;
    public float TrailInterval => trailInterval;
    public int LiveCount => live.Count;
    public int Capacity => maxElements;

    class Element
    {
        public Transform tf;
        public SpriteRenderer sr;
        public Vector2 pos, velocity, startScale, endScale;
        public Color color;
        public float age, life, rotation, spin;
    }
    readonly List<Element> live = new List<Element>(256);
    private readonly Stack<Element> free = new Stack<Element>(256);

    void Awake()
    {
        instance = this;
        ProcSprites.Ensure();
        if (glowMaterial != null) ProcSprites.GlowMaterial = glowMaterial;
        for (int i = 0; i < Mathf.Min(prewarm, maxElements); i++) free.Push(CreateElement());
    }


    public void Configure(Material material)
    {
        glowMaterial = material;
        ProcSprites.GlowMaterial = material;
    }

    private Element CreateElement()
    {
        GameObject go = new GameObject("Arcane Accent");
        go.transform.SetParent(transform, false);
        Element e = new Element { tf = go.transform, sr = go.AddComponent<SpriteRenderer>() };
        go.SetActive(false);
        return e;
    }

    public void Emit(Sprite sprite, Vector2 pos, Vector2 startScale, Vector2 endScale,
        float rotation, float spin, Color color, float life, Vector2 velocity, int order = 12)
    {
        if (live.Count >= maxElements || sprite == null || life <= 0f) return;
        Element e = free.Count > 0 ? free.Pop() : CreateElement();
        e.pos = pos; e.velocity = velocity; e.startScale = startScale; e.endScale = endScale;
        e.rotation = rotation; e.spin = spin; e.color = color; e.life = life; e.age = 0f;
        e.sr.sprite = sprite; e.sr.color = color; e.sr.sortingOrder = order;
        e.sr.sharedMaterial = ProcSprites.GlowMaterial != null ? ProcSprites.GlowMaterial : ProcSprites.Material;
        e.sr.enabled = true; e.sr.flipX = e.sr.flipY = false;
        e.tf.SetPositionAndRotation(new Vector3(pos.x, pos.y, 0f), Quaternion.Euler(0f, 0f, rotation));
        e.tf.localScale = new Vector3(startScale.x, startScale.y, 1f);
        e.tf.gameObject.SetActive(true);
        live.Add(e);
    }

    public void Cast(Vector2 pos, Color color, float radius = 0.5f)
    {
        Emit(ProcSprites.Ring, pos, Vector2.one * radius * 0.3f, Vector2.one * radius,
            0f, 0f, Tint(color, 0.65f), 0.22f, Vector2.zero, 5);
        Emit(ProcSprites.Star, pos, Vector2.one * 0.2f, Vector2.one * 0.65f,
            45f, -90f, Tint(Color.Lerp(color, Color.white, 0.6f), 0.75f), 0.16f, Vector2.zero);
    }

    public void Impact(Vector2 pos, Color color, float strength = 0.4f)
    {
        Emit(ProcSprites.Star, pos, Vector2.one * strength, Vector2.one * strength * 1.8f,
            Random.Range(0f, 90f), 100f, Tint(Color.Lerp(color, Color.white, 0.65f), 0.9f), 0.18f, Vector2.zero);
        for (int i = 0; i < 3; i++)
        {
            float angle = Random.Range(0f, 360f);
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            Emit(ProcSprites.Wisp, pos, new Vector2(0.2f, 0.3f), new Vector2(0.08f, 0.05f), angle,
                0f, Tint(color, 0.65f), 0.22f, dir * 2f);
        }
    }


    public void Shockwave(Vector2 pos, Color color, float radius)
    {
        Emit(ProcSprites.Ring, pos, Vector2.one * radius * 0.15f, Vector2.one * radius,
            0f, 0f, Tint(color, 0.85f), 0.3f, Vector2.zero, 4);
        Emit(ProcSprites.Segments, pos, Vector2.one * radius * 0.3f, Vector2.one * radius * 0.95f,
            Random.Range(0f, 360f), 80f, Tint(color, 0.45f), 0.4f, Vector2.zero, 3);
        Impact(pos, color, Mathf.Min(radius, 1f));
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f + Random.Range(-12f, 12f);
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            Emit(ProcSprites.Star, pos + dir * radius * 0.2f, Vector2.one * 0.2f, Vector2.zero,
                angle, 100f, Tint(color, 0.75f), 0.35f, dir * radius * 2f);
        }
    }
    public void Trail(Sprite sprite, Vector2 pos, Vector2 scale, float rotation, Color color)
    {
        Emit(sprite, pos, scale, scale * 0.35f, rotation, 0f, Tint(color, 0.35f), 0.2f, Vector2.zero, 8);
    }

    public static Color Tint(Color color, float alpha) { color.a = alpha; return color; }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        for (int i = live.Count - 1; i >= 0; i--)
        {
            Element e = live[i];
            e.age += dt;
            if (e.age >= e.life)
            {
                e.tf.gameObject.SetActive(false); free.Push(e);
                int last = live.Count - 1; live[i] = live[last]; live.RemoveAt(last);
                continue;
            }
            float t = e.age / e.life;
            e.pos += e.velocity * dt; e.rotation += e.spin * dt;
            e.tf.SetPositionAndRotation(new Vector3(e.pos.x, e.pos.y, 0f), Quaternion.Euler(0f, 0f, e.rotation));
            Vector2 scale = Vector2.Lerp(e.startScale, e.endScale, 1f - (1f - t) * (1f - t));
            e.tf.localScale = new Vector3(scale.x, scale.y, 1f);
            e.sr.color = Tint(e.color, e.color.a * (1f - t) * (1f - t));
        }
    }
    void OnDestroy()
    {
        if (instance != this) return;
        instance = null;
        ProcSprites.Release();
    }
}