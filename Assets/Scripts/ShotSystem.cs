using System.Collections.Generic;
using UnityEngine;

public class ShotSystem : MonoBehaviour
{
    private static ShotSystem instance;
    public static ShotSystem Instance
    {
        get
        {
            if (instance == null) instance = new GameObject("ShotSystem").AddComponent<ShotSystem>();
            return instance;
        }
    }
    public Transform Owner;
    const int MaxLive = 500;

    private class Shot
    {
        public Transform tf;
        public SpriteRenderer sr;
        public GameObject fxGo;
        public SpriteRenderer fx, core;
        public SigilKind source;
        public float trailTimer, size;
        public Vector2 pos, vel, dir, aim;
        public float radius, damage, life, age, speed, length, outTime, homing, homingDelay, fxTimer;
        public int pierceLeft, pierceMax;
        public bool crackle, returning;
        public ShotMotion motion;
        public Color color;
        public float burstRadius, burstDamage, craterLife, craterTick, craterDamage;
        public readonly List<EnemyChase> hit = new List<EnemyChase>(8);
    }

    readonly List<Shot> live = new List<Shot>(128);
    readonly Stack<Shot> free = new Stack<Shot>(128);
    void Awake() { instance = this; ProcSprites.Ensure(); }

    public int Count(SigilKind source)
    {
        int n = 0;
        for (int i = 0; i < live.Count; i++) if (live[i].source == source) n++;
        return n;
    }
    Shot CreateShot()
    {
        Shot s = new Shot();
        Material mat = ProcSprites.Material;
        GameObject go = new GameObject("Shot");
        go.transform.SetParent(transform, false);
        s.tf = go.transform;
        s.sr = go.AddComponent<SpriteRenderer>();
        s.sr.sprite = ProcSprites.Bolt;
        s.sr.sortingOrder = 10;
        if (mat != null) s.sr.sharedMaterial = mat;
        s.fxGo = new GameObject("Crackle");
        s.fxGo.transform.SetParent(go.transform, false);
        s.fx = s.fxGo.AddComponent<SpriteRenderer>();
        s.fx.sprite = ProcSprites.Lightning[0];
        s.fx.sortingOrder = 11;
        if (mat != null) s.fx.sharedMaterial = mat;
        GameObject coreGo = new GameObject("Bright Core");
        coreGo.transform.SetParent(go.transform, false);
        s.core = coreGo.AddComponent<SpriteRenderer>();
        s.core.sortingOrder = 12;
        return s;
    }
    public void Fire(Vector2 origin, Vector2 dir, ShotSpec spec)
    {
        if (live.Count >= MaxLive) return;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();
        Shot s = free.Count > 0 ? free.Pop() : CreateShot();
        s.pos = origin; s.dir = dir; s.speed = spec.speed;
        s.vel = dir * spec.speed; s.radius = spec.radius;
        s.damage = spec.damage; s.life = spec.lifetime; s.age = 0f;
        s.pierceLeft = spec.pierce; s.pierceMax = spec.pierce;
        s.motion = spec.motion; s.length = spec.length;
        s.outTime = spec.outTime; s.homing = spec.homing;
        s.homingDelay = spec.homingDelay; s.aim = spec.aim;
        s.color = spec.color; s.returning = false;
        s.burstRadius = spec.burstRadius; s.burstDamage = spec.burstDamage;
        s.craterLife = spec.craterLife; s.craterTick = spec.craterTick;
        s.craterDamage = spec.craterDamage;
        s.hit.Clear();
        s.crackle = (spec.flags & ShotFlags.Crackle) != 0;
        s.fxTimer = 0f;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        s.tf.SetPositionAndRotation(new Vector3(origin.x, origin.y, 0f), Quaternion.Euler(0f, 0f, angle));
        if (spec.motion == ShotMotion.Lance)
            s.tf.localScale = new Vector3(0.8f, 0.8f, 1f);
        else if (spec.motion == ShotMotion.Boomerang)
            s.tf.localScale = Vector3.one * (spec.size * 1.25f);
        else
            s.tf.localScale = Vector3.one * spec.size;
        if (spec.motion == ShotMotion.Boomerang)
            s.sr.sprite = ProcSprites.Boomerang ?? ProcSprites.Crescent;
        else if (spec.motion == ShotMotion.Lance)
            s.sr.sprite = ProcSprites.Lance ?? ProcSprites.Bolt;
        else if (spec.round)
            s.sr.sprite = ProcSprites.Orb;
        else
            s.sr.sprite = ProcSprites.Bolt;

        s.sr.color = spec.color;
        s.sr.sharedMaterial = ProcSprites.GlowMaterial ?? ProcSprites.Material;
        s.sr.flipX = s.sr.flipY = false;
        s.source = spec.source; s.size = spec.size; s.trailTimer = 0f;
        s.core.sprite = s.sr.sprite;
        s.core.sharedMaterial = s.sr.sharedMaterial;
        s.core.color = SigilVfxSystem.Tint(Color.Lerp(spec.color, Color.white, 0.75f), 0.8f);
        s.core.transform.localPosition = Vector3.zero;
        s.core.transform.localRotation = Quaternion.identity;
        s.core.transform.localScale = (spec.motion == ShotMotion.Lance) ? new Vector3(0.9f, 0.4f, 1f) : new Vector3(0.8f, 0.4f, 1f);
        s.core.enabled = true;
        s.fxGo.SetActive(s.crackle);
        if (s.crackle) s.fx.color = Color.Lerp(spec.color, Color.white, 0.6f);

        s.tf.gameObject.SetActive(true);
        live.Add(s);
        SigilVfxSystem.Instance.Cast(origin, spec.color, spec.motion == ShotMotion.Lance ? 0.7f : 0.35f);
    }
    private void Update()
    {
        if (live.Count == 0) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        EnemyView.Refresh();
        for (int i = live.Count - 1; i >= 0; i--)
        {
            Shot s = live[i];
            s.age += dt;
            s.life -= dt;
            bool dead = s.life <= 0f;


            switch (s.motion)
            {
                case ShotMotion.Boomerang:
                    if (!s.returning && s.age >= s.outTime)
                    { s.returning = true; s.hit.Clear(); s.pierceLeft = s.pierceMax; }
                    if (s.returning)
                    {
                        s.life = Mathf.Max(s.life, 1f);
                        if (Owner == null) { dead = true; }
                        else
                        {
                            Vector2 to = (Vector2)Owner.position - s.pos;
                            if (to.sqrMagnitude < 0.25f)
                            { dead = true; s.burstRadius = 0f; SigilVfxSystem.Instance.Impact(s.pos, s.color, 0.25f); }
                            else s.vel = to.normalized * (s.speed * 1.35f);
                        }
                    }
                    s.tf.Rotate(0f, 0f, 960f * dt);
                    break;

                case ShotMotion.Lance:
                    if (Owner != null)
                    {
                        float total = s.age + s.life;
                        float u = Mathf.Clamp01(s.age / Mathf.Max(0.01f, total));
                        float extension;
                        if (u < 0.25f) extension = Mathf.SmoothStep(0f, s.length, u / 0.25f);
                        else if (u < 0.55f) extension = s.length;
                        else extension = Mathf.SmoothStep(s.length, 0f, (u - 0.55f) / 0.45f);
                        s.pos = (Vector2)Owner.position + s.dir * (0.5f + extension);
                    }
                    break;
            }

            if (s.homing > 0f && s.age >= s.homingDelay) Steer(s, dt);
            if (s.motion != ShotMotion.Lance) s.pos += s.vel * dt;

            if (s.motion == ShotMotion.Comet && Vector2.Dot(s.aim - s.pos, s.vel) <= 0f)
            { s.pos = s.aim; dead = true; }

            if (!dead && s.motion != ShotMotion.Comet) dead = CheckHits(s);
            if (dead) { if (s.burstRadius > 0f) Explode(s); Release(i); continue; }

            Vector2 drawPos = s.pos;
            s.tf.position = new Vector3(drawPos.x, drawPos.y, 0f);
            if (s.homing > 0f)
                s.tf.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(s.vel.y, s.vel.x) * Mathf.Rad2Deg);
            else if (s.motion == ShotMotion.Lance)
                s.tf.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(s.dir.y, s.dir.x) * Mathf.Rad2Deg);

            s.trailTimer -= dt;
            if (s.trailTimer <= 0f)
            {
                s.trailTimer = SigilVfxSystem.Instance.TrailInterval;
                float a = Mathf.Atan2(s.vel.y, s.vel.x) * Mathf.Rad2Deg;
                if (s.motion == ShotMotion.Boomerang || s.motion == ShotMotion.Lance)
                    SigilVfxSystem.Instance.Trail(s.sr.sprite, drawPos,
                        new Vector2(s.tf.localScale.x, s.tf.localScale.y), s.tf.eulerAngles.z,
                        s.returning ? Color.Lerp(s.color, Color.white, 0.35f) : s.color);
                else
                {
                    Vector2 tail = s.pos - s.vel.normalized * s.size * 0.4f;
                    SigilVfxSystem.Instance.Trail(ProcSprites.Wisp, tail,
                        new Vector2(s.size * 1.2f, s.size * 0.6f), a, s.color);
                    if (s.motion == ShotMotion.Comet)
                        SigilVfxSystem.Instance.Emit(ProcSprites.Star, tail + Random.insideUnitCircle * 0.25f,
                            Vector2.one * 0.18f, Vector2.zero, 45f, 60f, s.color, 0.3f, Vector2.up * 0.5f);
                }
            }
            s.core.color = SigilVfxSystem.Tint(Color.Lerp(s.color, Color.white, 0.75f),
                0.65f + 0.15f * Mathf.Sin(s.age * 25f));


            if (s.crackle)
            {
                s.fxTimer -= dt;
                if (s.fxTimer <= 0f)
                {
                    s.fxTimer = 0.05f;
                    s.fx.sprite = ProcSprites.Lightning[Random.Range(0, ProcSprites.LightningFrames)];
                    s.fx.flipY = Random.value < 0.5f;
                }
            }
        }
    }

    bool CheckHits(Shot s)
    {
        bool capsule = s.length > 0f;
        float lanceReach = (192f / 64f) * s.tf.localScale.x;
        Vector2 end = capsule ? s.pos + s.dir * lanceReach : s.pos;
        for (int k = 0; k < EnemyView.Count; k++)
        {
            float reach = s.radius + EnemyView.Rad[k];
            float sqr = capsule ? SegDistSqr(EnemyView.Pos[k], s.pos, end)
                                : (EnemyView.Pos[k] - s.pos).sqrMagnitude;
            if (sqr > reach * reach || !EnemyView.IsAlive(k)) continue;
            EnemyChase target = EnemyView.List[k];
            if (s.hit.Contains(target)) continue;
            s.hit.Add(target);
            SigilVfxSystem.Instance.Impact(EnemyView.Pos[k], s.color, 0.3f);
            Damage.Hit(target, s.damage);
            if (s.pierceLeft <= 0) return true;
            s.pierceLeft--;
        }
        return false;
    }

    private void Steer(Shot s, float dt)
    {
        int target = EnemyView.Nearest(s.pos, 12f);
        if (target < 0) return;
        Vector2 want = EnemyView.Pos[target] - s.pos;
        float cur = Mathf.Atan2(s.vel.y, s.vel.x) * Mathf.Rad2Deg;
        float goal = Mathf.Atan2(want.y, want.x) * Mathf.Rad2Deg;
        float step = Mathf.Clamp(Mathf.DeltaAngle(cur, goal), -s.homing * dt, s.homing * dt);
        float next = (cur + step) * Mathf.Deg2Rad;
        s.vel = new Vector2(Mathf.Cos(next), Mathf.Sin(next)) * s.speed;
    }
    void Explode(Shot s)
    {
        ZoneSystem zones = ZoneSystem.Instance;
        zones.Burst(s.pos, s.burstRadius, s.burstDamage, s.color, false);
        if (s.craterLife > 0f)
            zones.Spawn(new ZoneSpec
            {
                style = ZoneStyle.Rift,
                source = SigilKind.Comet,
                pos = s.pos,
                color = s.color,
                radius = s.burstRadius * 0.85f,
                life = s.craterLife,
                tickEvery = s.craterTick,
                tickDamage = s.craterDamage
            });
    }

    void Release(int index)
    {
        Shot s = live[index]; s.tf.gameObject.SetActive(false); free.Push(s);
        int last = live.Count - 1; live[index] = live[last]; live.RemoveAt(last);
    }

    private static float SegDistSqr(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
        return (p - (a + ab * t)).sqrMagnitude;
    }
}