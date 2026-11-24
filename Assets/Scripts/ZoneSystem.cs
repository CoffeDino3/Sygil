using System.Collections.Generic;
using UnityEngine;

public enum ZoneStyle
{
    Area, Rift, Blade, Cloud, Mine, Seed, Totem, Familiar, Decoy, Flash, Tether,
    Circle, Bug,
}


public struct ZoneSpec
{
    public ZoneStyle style;
    public SigilKind source;
    public Vector2 pos;
    public Color color;
    public float radius, growTo, life;
    public float tickEvery, tickDamage, slow, push;
    public bool hitOnce, followOwner;
    public float orbitRadius, orbitSpeed, orbitAngle;
    public Vector2 drift;
    public float burstDamage, burstRadius;
    public bool burstOnExpire;
    public float triggerRadius, armDelay;
    public float shootEvery, shootRange;
    public ShotSpec shot;
    public float strikeEvery, strikeDamage, strikeRadius;
    public bool decoy;
    public float sweepFrom, sweepTo, sweepReach, sweepWidth;
    public EnemyChase linkA, linkB;
    public bool lightning;
    public EnemyChase seedTarget;
    public float seedSpeed, seedRange, seedFlightLife;

    public float auraDamage, auraCast;
    public float walkSpeed, seekRange, leash, contactDamage, contactEvery;
    public float health;
}
public class ZoneSystem : MonoBehaviour
{
    static ZoneSystem instance;
    public static ZoneSystem Instance
    {
        get
        {
            if (instance == null) instance = new GameObject("ZoneSystem").AddComponent<ZoneSystem>();
            return instance;
        }
    }


    public static readonly List<Vector2> DecoyPositions = new List<Vector2>();
    public static float AuraDamage;
    public static float AuraCast;
    public Transform Owner;
    const int MaxZones = 160;

    class Zone
    {
        public ZoneSpec spec;
        public float age, tickTimer, shootTimer, strikeTimer, rotation, frameTimer;
        public int frame;
        public Transform tf;
        public SpriteRenderer fill, edge, mark, accent, detail;
        public float visualTimer;
        public EnemyChase seedHost;
        public Vector2 launchPos, seedOffset;
        public float fuseAge;
        public bool attached;
        public Vector2 wanderDir;
        public float wanderTimer, contactTimer, heading;
        public float hp, hurtTimer, hurtFlash;
        public readonly List<EnemyChase> hit = new List<EnemyChase>(16);
    }

    readonly List<Zone> live = new List<Zone>(64);
    private readonly Stack<Zone> free = new Stack<Zone>(64);

    void Awake()
    {
        instance = this;
        ProcSprites.Ensure();
        Damage.ClearLinks();
        DecoyPositions.Clear();
        AuraDamage = AuraCast = 0f;
    }

    Zone CreateZone()
    {
        Zone z = new Zone();
        GameObject go = new GameObject("Zone");
        go.transform.SetParent(transform, false);
        z.tf = go.transform;
        z.fill = NewRenderer("Fill", z.tf);
        z.edge = NewRenderer("Edge", z.tf);
        z.mark = NewRenderer("Mark", z.tf);
        z.accent = NewRenderer("Accent", z.tf);
        z.detail = NewRenderer("Detail", z.tf);
        return z;
    }


    private static SpriteRenderer NewRenderer(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Material mat = ProcSprites.Material;
        if (mat != null) sr.sharedMaterial = mat;
        return sr;
    }
    public void Spawn(ZoneSpec spec) { TrySpawn(spec); }
    public bool TrySpawn(ZoneSpec spec)
    {
        if (live.Count >= MaxZones) return false;
        Zone z = free.Count > 0 ? free.Pop() : CreateZone();
        z.spec = spec;
        z.seedHost = null; z.attached = false; z.fuseAge = 0f;
        z.launchPos = spec.pos; z.seedOffset = Vector2.zero;
        z.age = 0f;
        z.tickTimer = spec.tickEvery;
        z.shootTimer = Mathf.Min(0.25f, spec.shootEvery);
        z.strikeTimer = spec.strikeEvery * 0.5f;
        z.rotation = Random.Range(0f, 360f);
        z.frame = Random.Range(0, ProcSprites.LightningFrames);
        z.frameTimer = 0f;
        z.hit.Clear();
        z.wanderDir = Random.insideUnitCircle.normalized;
        z.wanderTimer = 0f; z.contactTimer = 0f;
        z.hp = spec.health; z.hurtTimer = 0f; z.hurtFlash = 0f;
        z.heading = Mathf.Atan2(z.wanderDir.y, z.wanderDir.x) * Mathf.Rad2Deg;

        bool above = spec.style == ZoneStyle.Blade || spec.style == ZoneStyle.Totem ||
                     spec.style == ZoneStyle.Familiar || spec.style == ZoneStyle.Decoy ||
                     spec.style == ZoneStyle.Tether || spec.style == ZoneStyle.Bug;
        z.fill.sortingOrder = -6;
        z.edge.sortingOrder = -5;
        z.mark.sortingOrder = above ? 6 : -4;
        if (spec.lightning) z.mark.sortingOrder = 7;
        z.accent.sortingOrder = above ? 7 : -3;
        z.detail.sortingOrder = above ? 8 : -2;
        Material glow = ProcSprites.GlowMaterial != null ? ProcSprites.GlowMaterial : ProcSprites.Material;
        z.fill.sharedMaterial = ProcSprites.Material;
        z.edge.sharedMaterial = z.mark.sharedMaterial = z.accent.sharedMaterial = z.detail.sharedMaterial = glow;
        z.visualTimer = 0f;

        if (spec.style == ZoneStyle.Tether) Damage.Link(spec.linkA, spec.linkB);

        z.tf.gameObject.SetActive(true);
        live.Add(z);
        Draw(z, spec.radius, 0f);
        if (spec.style != ZoneStyle.Flash) SigilVfxSystem.Instance.Cast(spec.pos, spec.color, Mathf.Min(spec.radius, 0.8f));
        return true;
    }
    public void Burst(Vector2 pos, float radius, float damage, Color color, bool lightning, float knockback = 5f)
    {
        EnemyView.Refresh();
        for (int k = 0; k < EnemyView.Count; k++)
        {
            Vector2 d = EnemyView.Pos[k] - pos;
            float reach = radius + EnemyView.Rad[k];
            if (d.sqrMagnitude > reach * reach) continue;
            if (!EnemyView.IsAlive(k)) continue;
            EnemyChase e = EnemyView.List[k];
            Damage.Hit(e, damage);
            e.Knockback(d.normalized * knockback);
        }
        SigilVfxSystem.Instance.Shockwave(pos, color, radius);
        Spawn(new ZoneSpec
        {
            style = ZoneStyle.Flash,
            source = (SigilKind)(-1),
            pos = pos,
            color = color,
            radius = radius * 0.4f,
            growTo = radius,
            life = 0.3f,
            lightning = lightning
        });
    }

    public int Count(SigilKind source)
    {
        int n = 0;
        for (int i = 0; i < live.Count; i++) if (live[i].spec.source == source) n++;
        return n;
    }

    private void Update()
    {
        DecoyPositions.Clear();
        if (live.Count == 0) { AuraDamage = AuraCast = 0f; return; }
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        AuraDamage = AuraCast = 0f;
        EnemyView.Refresh();
        Vector2 ownerPos = Owner != null ? (Vector2)Owner.position : Vector2.zero;

        for (int i = live.Count - 1; i >= 0; i--)
        {
            Zone z = live[i];
            ref ZoneSpec sp = ref z.spec;
            z.age += dt;
            bool end = false;
            if (sp.style == ZoneStyle.Seed)
            {
                int state = UpdateSeed(z, dt);
                if (state != 0)
                {
                    ZoneSpec finished = sp; Release(i);
                    if (state == 2) Burst(finished.pos, finished.burstRadius, finished.burstDamage, finished.color, false);
                    else SigilVfxSystem.Instance.Impact(finished.pos, finished.color, 0.15f);
                }
                else Draw(z, sp.radius, z.attached ? Mathf.Clamp01(z.fuseAge / Mathf.Max(0.01f, sp.life)) : 0f);
                continue;
            }
            if (sp.followOwner && Owner != null) sp.pos = ownerPos;
            if (sp.orbitRadius > 0f && Owner != null)
            {
                sp.orbitAngle += sp.orbitSpeed * dt;
                float a = sp.orbitAngle * Mathf.Deg2Rad;
                sp.pos = ownerPos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp.orbitRadius;
            }
            if (sp.drift != Vector2.zero)
            {
                sp.pos += sp.drift * dt;
                sp.drift = Rotate(sp.drift, Mathf.Sin(z.age * 0.7f) * 25f * dt);
            }
            float t = Mathf.Clamp01(z.age / Mathf.Max(0.01f, sp.life));
            float r = sp.growTo > 0f ? Mathf.Lerp(sp.radius, sp.growTo, t) : sp.radius;
            if (sp.decoy) DecoyPositions.Add(sp.pos);

            if (sp.style == ZoneStyle.Circle && Owner != null &&
                (ownerPos - sp.pos).sqrMagnitude <= r * r)
            { AuraDamage += sp.auraDamage; AuraCast += sp.auraCast; }

            if (sp.style == ZoneStyle.Bug && UpdateBug(z, dt, ownerPos)) end = true;

            if (sp.style == ZoneStyle.Tether)
            {
                EnemyChase a = sp.linkA, b = sp.linkB;
                if (a == null || b == null || a.Health == null || b.Health == null ||
                    a.Health.Current <= 0f || b.Health.Current <= 0f) { end = true; }
                else
                {
                    sp.pos = (a.Position + b.Position) * 0.5f;
                    z.tickTimer -= dt;
                    if (z.tickTimer <= 0f && sp.tickEvery > 0f)
                    { z.tickTimer += sp.tickEvery; Damage.Hit(a, sp.tickDamage); }
                }
            }

            if (!end)
            {
                if (sp.slow > 0f)
                {
                    for (int k = 0; k < EnemyView.Count; k++)
                    {
                        float dist = (EnemyView.Pos[k] - sp.pos).magnitude;
                        float rad = EnemyView.Rad[k];
                        if (sp.source == SigilKind.Snare)
                        { if (dist >= r * 0.35f - rad && dist <= r + rad && EnemyView.IsAlive(k)) EnemyView.List[k].ApplySlow(sp.slow, 0.3f); }
                        else
                        { if (dist <= r + rad && EnemyView.IsAlive(k)) EnemyView.List[k].ApplySlow(sp.slow, 0.25f); }
                    }
                }

                if (sp.source == SigilKind.Ward)
                {
                    for (int k = 0; k < EnemyView.Count; k++)
                    {
                        float reach = r + EnemyView.Rad[k];
                        if ((EnemyView.Pos[k] - sp.pos).sqrMagnitude > reach * reach) continue;
                        if (!EnemyView.IsAlive(k)) continue;
                        Burst(sp.pos, sp.burstRadius, sp.burstDamage, sp.color, false, sp.push);
                        end = true; break;
                    }
                }
                if (sp.tickEvery > 0f && sp.style != ZoneStyle.Tether && sp.source != SigilKind.Ward)
                {
                    z.tickTimer -= dt;
                    if (z.tickTimer <= 0f) { z.tickTimer += sp.tickEvery; TickArea(z, r); }
                }
                if (sp.hitOnce) HitOnce(z, r);
                if (sp.style == ZoneStyle.Blade) Sweep(z, t);

                if (sp.triggerRadius > 0f && z.age >= sp.armDelay)
                {
                    for (int k = 0; k < EnemyView.Count; k++)
                    {
                        float reach = sp.triggerRadius + EnemyView.Rad[k];
                        if ((EnemyView.Pos[k] - sp.pos).sqrMagnitude > reach * reach) continue;
                        if (!EnemyView.IsAlive(k)) continue;
                        Burst(sp.pos, sp.burstRadius, sp.burstDamage, sp.color, false);
                        end = true; break;
                    }
                }
                if (sp.shootEvery > 0f)
                {
                    z.shootTimer -= dt;
                    if (z.shootTimer <= 0f)
                    {
                        int target = EnemyView.Nearest(sp.pos, sp.shootRange);
                        if (target >= 0) { ShotSystem.Instance.Fire(sp.pos, EnemyView.Pos[target] - sp.pos, sp.shot); z.shootTimer = sp.shootEvery; }
                        else z.shootTimer = 0.2f;
                    }
                }

                if (sp.strikeEvery > 0f)
                {
                    z.strikeTimer -= dt;
                    if (z.strikeTimer <= 0f) { z.strikeTimer = sp.strikeEvery; Strike(z, r); }
                }
                if (z.age >= sp.life)
                { if (sp.burstOnExpire) Burst(sp.pos, sp.burstRadius, sp.burstDamage, sp.color, false); end = true; }
            }

            if (end) { Release(i); continue; }
            Draw(z, r, t);
        }
    }
    static bool SeedTargetAlive(EnemyChase target)
    { return target != null && target.isActiveAndEnabled && target.Health != null && target.Health.Current > 0f; }
    public static bool SeedContact(Vector2 start, Vector2 end, Vector2 center, float radius, out float fraction)
    {
        fraction = 0f;
        Vector2 offset = start - center;
        float c = offset.sqrMagnitude - radius * radius;
        if (c <= 0f) return true;
        Vector2 delta = end - start;
        float a = delta.sqrMagnitude;
        if (a < 1e-8f) return false;
        float b = Vector2.Dot(offset, delta);
        float discriminant = b * b - a * c;
        if (discriminant < 0f) return false;
        fraction = (-b - Mathf.Sqrt(discriminant)) / a;
        return fraction >= 0f && fraction <= 1f;
    }


    int UpdateSeed(Zone z, float dt)
    {
        ref ZoneSpec sp = ref z.spec;
        if (z.attached)
        {
            if (z.seedHost != null) sp.pos = z.seedHost.Position + z.seedOffset;
            if (!SeedTargetAlive(z.seedHost)) return 2;
            z.fuseAge += dt;
            return z.fuseAge >= sp.life ? 2 : 0;
        }
        if (z.age > sp.seedFlightLife) return 1;
        if (!SeedTargetAlive(sp.seedTarget))
        {
            sp.seedTarget = null;
            float best = float.PositiveInfinity;
            for (int k = 0; k < EnemyView.Count; k++)
            {
                if (!SeedTargetAlive(EnemyView.List[k])) continue;
                if ((EnemyView.Pos[k] - z.launchPos).sqrMagnitude > sp.seedRange * sp.seedRange) continue;
                float d = (EnemyView.Pos[k] - sp.pos).sqrMagnitude;
                if (d < best) { best = d; sp.seedTarget = EnemyView.List[k]; }
            }
            if (sp.seedTarget == null) return 1;
        }
        Vector2 start = sp.pos;
        Vector2 to = sp.seedTarget.Position - start;
        Vector2 end = start + Vector2.ClampMagnitude(to, sp.seedSpeed * dt);
        float first = 2f; EnemyChase host = null;
        for (int k = 0; k < EnemyView.Count; k++)
        {
            if (!SeedTargetAlive(EnemyView.List[k])) continue;
            if (SeedContact(start, end, EnemyView.Pos[k], sp.radius + EnemyView.Rad[k], out float f) && f < first)
            { first = f; host = EnemyView.List[k]; }
        }
        if (host != null)
        {
            sp.pos = Vector2.Lerp(start, end, first);
            z.seedOffset = Vector2.ClampMagnitude(sp.pos - host.Position, host.HitRadius);
            sp.pos = host.Position + z.seedOffset;
            z.seedHost = host; z.attached = true; z.fuseAge = 0f;
            SigilVfxSystem.Instance.Impact(sp.pos, sp.color, 0.35f);
        }
        else sp.pos = end;
        return 0;
    }
    const float BugHurt = 1f;
    private const float BugHurtEvery = 0.6f;

    private bool UpdateBug(Zone z, float dt, Vector2 ownerPos)
    {
        ref ZoneSpec sp = ref z.spec;
        Vector2 dir;
        Vector2 home = ownerPos - sp.pos;
        int prey = EnemyView.Nearest(sp.pos, sp.seekRange);
        if (Owner != null && home.sqrMagnitude > sp.leash * sp.leash) dir = home.normalized;
        else if (prey >= 0) dir = (EnemyView.Pos[prey] - sp.pos).normalized;
        else
        {
            z.wanderTimer -= dt;
            if (z.wanderTimer <= 0f)
            {
                z.wanderTimer = Random.Range(0.5f, 1.3f);
                z.wanderDir = Random.insideUnitCircle.normalized;
            }
            dir = z.wanderDir;
        }

        dir = Rotate(dir, Mathf.Sin(z.age * 9f + z.rotation) * 25f);
        sp.pos += dir * sp.walkSpeed * dt;
        if (dir.sqrMagnitude > 0.0001f)
            z.heading = Mathf.LerpAngle(z.heading, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 12f * dt);


        z.contactTimer -= dt;
        z.hurtTimer -= dt;
        z.hurtFlash -= dt;
        bool ready = z.contactTimer <= 0f;
        bool bit = false, touched = false;
        for (int k = 0; k < EnemyView.Count; k++)
        {
            float reach = sp.radius + EnemyView.Rad[k];
            if ((EnemyView.Pos[k] - sp.pos).sqrMagnitude > reach * reach || !EnemyView.IsAlive(k)) continue;
            touched = true;
            if (!ready) continue;
            EnemyChase e = EnemyView.List[k];
            Damage.Hit(e, sp.contactDamage);
            e.Knockback(dir * 2f);
            SigilVfxSystem.Instance.Impact(EnemyView.Pos[k], sp.color, 0.25f);
            bit = true;
        }
        if (ready) z.contactTimer = bit ? sp.contactEvery : 0f;

        if (touched && sp.health > 0f && z.hurtTimer <= 0f)
        {
            z.hp -= BugHurt;
            z.hurtTimer = BugHurtEvery;
            z.hurtFlash = 0.15f;
            if (z.hp <= 0f)
            {
                SigilVfxSystem.Instance.Impact(sp.pos, sp.color, 0.5f);
                return true;
            }
        }
        return false;
    }

    private void TickArea(Zone z, float r)
    {
        ZoneSpec sp = z.spec;
        bool isRift = sp.style == ZoneStyle.Rift;
        Vector2 riftDir = isRift ? Rotate(Vector2.right, z.rotation) : Vector2.zero;
        Vector2 riftA = sp.pos - riftDir * r;
        Vector2 riftB = sp.pos + riftDir * r;
        for (int k = 0; k < EnemyView.Count; k++)
        {
            Vector2 p = EnemyView.Pos[k]; float rad = EnemyView.Rad[k];
            if (isRift) { if (ProcSprites.DistToSegment(p, riftA, riftB) > (r * 0.45f + rad)) continue; }
            else { Vector2 d = p - sp.pos; if (d.sqrMagnitude > (r + rad) * (r + rad)) continue; }
            if (!EnemyView.IsAlive(k)) continue;
            EnemyChase e = EnemyView.List[k];
            if (sp.tickDamage > 0f) Damage.Hit(e, sp.tickDamage);
            if (sp.push > 0f) e.Knockback((p - sp.pos).normalized * sp.push);
        }
    }

    void HitOnce(Zone z, float r)
    {
        ZoneSpec sp = z.spec;
        for (int k = 0; k < EnemyView.Count; k++)
        {
            Vector2 d = EnemyView.Pos[k] - sp.pos;
            float reach = r + EnemyView.Rad[k];
            if (d.sqrMagnitude > reach * reach || !EnemyView.IsAlive(k)) continue;
            EnemyChase e = EnemyView.List[k];
            if (z.hit.Contains(e)) continue;
            z.hit.Add(e);
            Damage.Hit(e, sp.tickDamage);
            if (sp.push > 0f) e.Knockback(d.normalized * sp.push);
        }
    }


    private void Sweep(Zone z, float t)
    {
        ZoneSpec sp = z.spec;
        float angle = Mathf.Lerp(sp.sweepFrom, sp.sweepTo, t);
        for (int k = 0; k < EnemyView.Count; k++)
        {
            Vector2 d = EnemyView.Pos[k] - sp.pos;
            float reach = sp.sweepReach + EnemyView.Rad[k];
            if (d.sqrMagnitude > reach * reach) continue;
            float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle(a, angle)) > sp.sweepWidth * 0.5f) continue;
            if (!EnemyView.IsAlive(k)) continue;
            EnemyChase e = EnemyView.List[k];
            if (z.hit.Contains(e)) continue;
            z.hit.Add(e);
            Damage.Hit(e, sp.tickDamage);
            e.Knockback(d.normalized * 4.5f);
        }
    }
    void Strike(Zone z, float r)
    {
        ZoneSpec sp = z.spec;
        int pick = EnemyView.RandomIn(sp.pos, r * 1.1f);
        Vector2 p = pick >= 0 ? EnemyView.Pos[pick] : sp.pos + Random.insideUnitCircle * (r * 0.8f);
        Burst(p, sp.strikeRadius, sp.strikeDamage, Color.Lerp(sp.color, Color.white, 0.65f), true);
    }
    void Release(int index)
    {
        Zone z = live[index];
        if (z.spec.style == ZoneStyle.Tether) Damage.Unlink(z.spec.linkA, z.spec.linkB);
        z.tf.gameObject.SetActive(false);
        free.Push(z);
        int last = live.Count - 1; live[index] = live[last]; live.RemoveAt(last);
    }
    private void Draw(Zone z, float r, float t)
    {
        ZoneSpec sp = z.spec;
        float left = sp.life - z.age;
        float vis = Mathf.Clamp01(z.age / 0.12f) * Mathf.Clamp01(left / 0.25f);

        z.tf.position = new Vector3(sp.pos.x, sp.pos.y, 0f);
        z.fill.enabled = z.edge.enabled = z.mark.enabled = z.accent.enabled = z.detail.enabled = false;
        z.mark.transform.localPosition = z.accent.transform.localPosition = z.detail.transform.localPosition = Vector3.zero;
        float spin = z.rotation + z.age * 35f;
        Color bright = Color.Lerp(sp.color, Color.white, 0.6f);
        bool emit = z.visualTimer <= z.age && z.age > 0f;
        if (emit) z.visualTimer = z.age + 0.12f;
        switch (sp.style)
        {
            case ZoneStyle.Area:
                if (sp.source == SigilKind.Ward)
                {
                    Show(z.mark, ProcSprites.AegisBarrier ?? ProcSprites.Ring,
                        r * 1.15f, r * 1.15f, spin * 0.5f, Alpha(bright, (0.55f + 0.15f * Mathf.Sin(z.age * 9f)) * vis));
                    Show(z.edge, ProcSprites.Ring, r, r, 0f, Alpha(sp.color, 0.7f * vis));
                    Show(z.accent, ProcSprites.AegisBarrier ?? ProcSprites.Segments,
                        r * 0.95f, r * 0.95f, -spin * 0.7f, Alpha(sp.color, 0.4f * vis));
                }
                else if (sp.source == SigilKind.Snare)
                {
                    Show(z.mark, ProcSprites.SnareBrambles ?? ProcSprites.Disc,
                        r * 1.1f, r * 1.1f, spin * 0.35f, Alpha(sp.color, 0.7f * vis));
                    Show(z.edge, ProcSprites.Ring, r, r, 0f, Alpha(sp.color, 0.5f * vis));
                    Show(z.detail, ProcSprites.SnareBrambles ?? ProcSprites.Disc,
                        r * 0.85f, r * 0.85f, -spin * 0.5f, Alpha(bright, 0.35f * vis));
                }
                else if (sp.source == SigilKind.Pulse)
                {
                    Show(z.fill, ProcSprites.Disc, r, r, 0f, Alpha(sp.color, 0.08f * (1f - t)));
                    Show(z.edge, ProcSprites.Ring, r, r, 0f, Alpha(sp.color, 0.8f * (1f - t)));
                    Show(z.detail, ProcSprites.Segments, r * 0.85f, r * 0.85f, spin, Alpha(bright, 0.65f * (1f - t)));
                }
                else
                {
                    Show(z.fill, ProcSprites.Disc, r, r, 0f, Alpha(sp.color, 0.07f * vis));
                    Show(z.edge, ProcSprites.Ring, r, r, 0f, Alpha(sp.color, 0.55f * vis));
                }
                break;

            case ZoneStyle.Rift:
                {
                    Color voidBlack = new Color(0.02f, 0.01f, 0.04f);
                    float pulse = 0.8f + 0.2f * Mathf.Sin(z.age * 8f);
                    float breathe = 1f + 0.04f * Mathf.Sin(z.age * 3f);
                    Show(z.fill, ProcSprites.Rift, r * 1.4f * breathe, r * 0.5f * breathe,
                        z.rotation, Alpha(voidBlack, 0.9f * vis));
                    Show(z.mark, ProcSprites.Rift, r * 1.5f * breathe, r * 0.55f * breathe,
                        z.rotation, Alpha(Color.Lerp(sp.color, Color.white, 0.5f), pulse * 0.65f * vis));
                    Show(z.accent, ProcSprites.Rift, r * 1.6f * breathe, r * 0.6f * breathe,
                        z.rotation, Alpha(sp.color, 0.25f * vis));
                    if (emit)
                    {
                        float side = Random.value < 0.5f ? -1f : 1f;
                        Vector2 escapeDir = Rotate(Vector2.right, z.rotation + 90f) * side;
                        Vector2 riftPt = sp.pos + Rotate(Vector2.right, z.rotation) * Random.Range(-r * 0.4f, r * 0.4f);
                        SigilVfxSystem.Instance.Emit(ProcSprites.Wisp, riftPt,
                            new Vector2(0.25f, 0.12f), new Vector2(0.06f, 0.03f),
                            z.rotation + (side > 0 ? 90f : -90f), 25f * side,
                            Alpha(sp.color, 0.5f), 0.5f, escapeDir * 1.2f, 2);
                    }
                    break;
                }


            case ZoneStyle.Cloud:
                {
                    float billow = 1f + 0.05f * Mathf.Sin(z.age * 2.5f);
                    Color cloudDark = Color.Lerp(sp.color, new Color(0.12f, 0.14f, 0.18f), 0.7f);
                    Show(z.fill, ProcSprites.Cloud ?? ProcSprites.Disc,
                        r * 1.2f * billow, r * 1.0f * billow,
                        z.rotation + Mathf.Sin(z.age * 0.5f) * 15f, Alpha(cloudDark, 0.55f * vis));
                    Show(z.edge, ProcSprites.Cloud ?? ProcSprites.Disc,
                        r * 0.95f, r * 0.85f, -z.rotation, Alpha(sp.color, 0.35f * vis));
                    float flashPow = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(z.age * 11f)), 12f);
                    if (flashPow > 0.01f)
                        Show(z.mark, ProcSprites.Cloud ?? ProcSprites.Disc,
                            r * 0.7f, r * 0.6f, z.rotation + 60f, Alpha(Color.white, flashPow * 0.55f * vis));
                    if (emit)
                        SigilVfxSystem.Instance.Emit(ProcSprites.Wisp, sp.pos + Random.insideUnitCircle * r * 0.5f,
                            Vector2.one * 0.3f, Vector2.one * 0.1f, Random.Range(0f, 360f), 20f,
                            Alpha(sp.color, 0.3f), 0.4f, Vector2.zero, 1);
                    break;
                }

            case ZoneStyle.Blade:
                {
                    float angle = Mathf.Lerp(sp.sweepFrom, sp.sweepTo, t);
                    Vector2 handPos = sp.pos + Rotate(Vector2.right, angle) * 0.5f;
                    z.tf.position = new Vector3(handPos.x, handPos.y, 0f);
                    const float swordScale = 0.85f;
                    Show(z.mark, ProcSprites.Sabre ?? ProcSprites.Arc,
                        swordScale, swordScale, angle, Alpha(Color.white, Mathf.Clamp01(left / 0.08f)));
                    Show(z.accent, ProcSprites.Sabre ?? ProcSprites.Arc,
                        swordScale * 1.02f, swordScale * 1.02f, angle, Alpha(sp.color, 0.75f * Mathf.Clamp01(left / 0.08f)));
                    if (emit)
                    {
                        Vector2 tip = handPos + Rotate(Vector2.right, angle) * (160f / 64f * swordScale * 0.85f);
                        SigilVfxSystem.Instance.Trail(ProcSprites.Wisp, tip, new Vector2(0.2f, 0.1f), angle + 90f, sp.color);
                    }
                    break;
                }
            case ZoneStyle.Mine:
                {
                    float armed = z.age >= sp.armDelay ? 1f : 0.45f;
                    Show(z.mark, ProcSprites.DiamondFrame ?? ProcSprites.Diamond,
                        0.5f, 0.5f, spin * 0.5f, Alpha(sp.color, 0.65f * armed * Mathf.Clamp01(left / 0.25f)));
                    Show(z.accent, ProcSprites.Diamond, 0.35f, 0.35f, -spin * 0.8f, Alpha(bright, 0.55f * armed));
                    Show(z.detail, ProcSprites.Star, 0.25f, 0.25f, 45f,
                        Alpha(bright, 0.6f * armed * (0.6f + 0.4f * Mathf.Sin(z.age * 8f))));
                    break;
                }
            case ZoneStyle.Seed:
                {
                    float pulse = 1f + (0.08f + 0.1f * t) * Mathf.Sin(z.fuseAge * (8f + 22f * t));
                    float size = z.attached ? 0.55f * pulse : 0.4f;
                    float alpha = Mathf.Clamp01(z.age / 0.08f);
                    float rot = z.attached ? 18f * Mathf.Sin(z.fuseAge * 5f) :
                        Mathf.Atan2(sp.seedTarget != null ? sp.seedTarget.Position.y - sp.pos.y : 1f,
                            sp.seedTarget != null ? sp.seedTarget.Position.x - sp.pos.x : 0f) * Mathf.Rad2Deg - 90f;
                    Show(z.mark, ProcSprites.SeedBody, size, size, rot, Alpha(sp.color, alpha));
                    Show(z.accent, ProcSprites.Orb, size * 0.4f, size * 0.4f, 0f, Alpha(bright, 0.9f * alpha * pulse));
                    z.mark.sortingOrder = 14; z.accent.sortingOrder = 15; z.detail.sortingOrder = 13;

                    if (z.attached)
                    {
                        Show(z.detail, ProcSprites.Star, 0.35f * pulse, 0.35f * pulse, spin * (1f + t * 4f),
                            Alpha(sp.color, 0.7f * alpha * t));
                        Show(z.edge, ProcSprites.Ring, sp.burstRadius, sp.burstRadius, 0f, Alpha(sp.color, 0.05f + 0.12f * t));
                        if (emit && t > 0.3f)
                            SigilVfxSystem.Instance.Emit(ProcSprites.Star, sp.pos + Random.insideUnitCircle * 0.15f,
                                Vector2.one * 0.15f, Vector2.zero, 45f, 90f, Alpha(bright, 0.7f), 0.15f, Vector2.up * 0.3f, 16);
                    }
                    else
                    {
                        Show(z.detail, ProcSprites.Wisp, size * 0.6f, size * 0.3f, rot + 90f, Alpha(sp.color, 0.5f * alpha));
                        if (emit) SigilVfxSystem.Instance.Trail(ProcSprites.Wisp, sp.pos, new Vector2(0.3f, 0.15f), rot + 90f, sp.color);
                    }
                    break;
                }

            case ZoneStyle.Totem:
                {
                    float bob = Mathf.Sin(z.age * 3.5f) * 0.08f;
                    Show(z.mark, ProcSprites.TotemPillar ?? ProcSprites.Diamond,
                        0.65f, 1.25f, 0f, Alpha(sp.color, vis));
                    Show(z.accent, ProcSprites.TotemPillar ?? ProcSprites.Diamond,
                        0.45f, 1.1f, 0f, Alpha(bright, (0.5f + 0.3f * Mathf.Sin(z.age * 8f)) * vis));
                    z.detail.transform.localPosition = new Vector3(0f, 0.75f + bob, 0f);
                    Show(z.detail, ProcSprites.Diamond, 0.28f, 0.28f, spin * 1.2f, Alpha(bright, 0.85f * vis));
                    if (emit)
                        SigilVfxSystem.Instance.Emit(ProcSprites.Star, sp.pos + new Vector2(0f, 0.75f + bob),
                            Vector2.one * 0.2f, Vector2.zero, 45f, 60f, Alpha(bright, 0.75f), 0.2f, Vector2.up * 0.3f, 9);
                    break;
                }
            case ZoneStyle.Familiar:
                {
                    float wingFlap = Mathf.Sin(z.age * 12f) * 0.12f;
                    float tilt = Mathf.Sin(z.age * 4f) * 12f;
                    Show(z.mark, ProcSprites.FamiliarCritter ?? ProcSprites.Orb,
                        0.75f + wingFlap, 0.75f, tilt, Alpha(sp.color, vis));
                    Show(z.accent, ProcSprites.FamiliarCritter ?? ProcSprites.Orb,
                        0.5f + wingFlap * 0.5f, 0.5f, tilt, Alpha(bright, 0.85f * vis));
                    if (emit)
                    {
                        Vector2 tailPos = sp.pos - Rotate(Vector2.up, tilt) * 0.25f;
                        SigilVfxSystem.Instance.Trail(ProcSprites.Wisp, tailPos, new Vector2(0.2f, 0.3f), tilt + 90f, sp.color);
                    }
                    break;
                }

            case ZoneStyle.Decoy:
                {
                    float sway = Mathf.Sin(z.age * 3.5f) * 6f;
                    float pulse = 1f + 0.05f * Mathf.Sin(z.age * 6f);
                    Show(z.mark, ProcSprites.DecoyPhantom ?? ProcSprites.Wisp,
                        0.7f * pulse, 0.95f * pulse, sway, Alpha(sp.color, 0.65f * vis));
                    Show(z.accent, ProcSprites.DecoyPhantom ?? ProcSprites.Orb,
                        0.5f, 0.75f, sway, Alpha(bright, 0.45f * vis));
                    if (emit)
                        SigilVfxSystem.Instance.Emit(ProcSprites.Wisp, sp.pos + Random.insideUnitCircle * 0.25f,
                            new Vector2(0.2f, 0.4f), Vector2.zero, 90f, 20f, Alpha(sp.color, 0.35f), 0.45f, Vector2.up * 0.4f, 5);
                    break;
                }

            case ZoneStyle.Circle:
                {
                    bool inside = Owner != null && ((Vector2)Owner.position - sp.pos).sqrMagnitude <= r * r;
                    float pop = Mathf.SmoothStep(0.55f, 1f, Mathf.Clamp01(z.age / 0.3f));
                    float rr = r * pop;
                    float glow = inside ? 1f : 0.65f;
                    float pulse = 0.85f + 0.15f * Mathf.Sin(z.age * 4f);
                    Show(z.fill, ProcSprites.Disc, rr, rr, 0f, Alpha(sp.color, (inside ? 0.16f : 0.08f) * vis));
                    Show(z.edge, ProcSprites.Ring, rr * (1f + 0.015f * Mathf.Sin(z.age * 5f)), rr * (1f + 0.015f * Mathf.Sin(z.age * 5f)),
                        0f, Alpha(sp.color, 0.7f * glow * vis));
                    Show(z.mark, ProcSprites.MagicCircleSigil ?? ProcSprites.Runes, rr, rr, spin * 0.2f,
                        Alpha(bright, 0.85f * glow * pulse * vis));
                    Show(z.accent, ProcSprites.Segments, rr * 0.62f, rr * 0.62f, -spin * 0.8f,
                        Alpha(sp.color, 0.55f * glow * vis));
                    float coreSize = rr * (0.3f + 0.05f * Mathf.Sin(z.age * 6f));
                    Show(z.detail, ProcSprites.Star, coreSize, coreSize, spin * 1.5f,
                        Alpha(Color.white, 0.6f * glow * vis));

                    if (emit)
                    {
                        Vector2 edgePt = sp.pos + Random.insideUnitCircle.normalized * rr * 0.95f;
                        SigilVfxSystem.Instance.Emit(ProcSprites.Star, edgePt, Vector2.one * 0.12f, Vector2.zero,
                            45f, 60f, Alpha(bright, 0.7f), 0.6f, Vector2.up * 0.7f);
                        if (inside)
                            SigilVfxSystem.Instance.Emit(ProcSprites.Star, sp.pos + Random.insideUnitCircle * rr * 0.7f,
                                Vector2.one * 0.16f, Vector2.zero, 45f, 90f, Alpha(Color.white, 0.8f), 0.5f, Vector2.up * 1.0f);
                    }
                    break;
                }

            case ZoneStyle.Bug:
                {
                    float s = Mathf.Max(0.3f, sp.radius * 2.6f);
                    float scuttle = Mathf.Sin(z.age * 30f);
                    float stretch = 1f + 0.06f * scuttle;
                    Vector2 fwd = Rotate(Vector2.right, z.heading);
                    Show(z.fill, ProcSprites.Disc, s * 0.38f, s * 0.22f, z.heading, Alpha(Color.black, 0.28f * vis));
                    float hpFrac = sp.health > 0f ? Mathf.Clamp01(z.hp / sp.health) : 1f;
                    Color bugCol = z.hurtFlash > 0f ? Color.Lerp(sp.color, new Color(1f, 0.25f, 0.25f), 0.85f) : sp.color;
                    Show(z.mark, ProcSprites.BugCritter ?? ProcSprites.Orb, s, s * stretch, z.heading,
                        Alpha(bugCol, vis * Mathf.Lerp(0.55f, 1f, hpFrac)));
                    Show(z.accent, ProcSprites.BugCritter ?? ProcSprites.Orb, s * 0.78f, s * 0.78f * stretch, z.heading,
                        Alpha(bright, 0.55f * vis));
                    if (emit)
                        SigilVfxSystem.Instance.Trail(ProcSprites.Wisp, sp.pos - fwd * s * 0.4f,
                            new Vector2(0.16f, 0.08f), z.heading, sp.color);
                    break;
                }
            case ZoneStyle.Flash:
                {
                    float f = 1f - t;
                    Show(z.fill, ProcSprites.Disc, r, r, 0f, Alpha(sp.color, 0.45f * f));
                    Show(z.edge, ProcSprites.Ring, r, r, 0f, Alpha(sp.color, 0.9f * f));
                    if (sp.lightning)
                    {
                        z.mark.transform.localPosition = new Vector3(0f, 1.75f, 0f);
                        Show(z.mark, ProcSprites.Lightning[z.frame], 2.4f, 1.3f, 90f, Alpha(Color.white, f));
                        z.accent.transform.localPosition = z.mark.transform.localPosition;
                        Show(z.accent, ProcSprites.Lightning[(z.frame + 2) % ProcSprites.LightningFrames], 2.4f, 0.55f, 90f, Alpha(sp.color, 0.7f * f));
                    }
                    else Show(z.mark, ProcSprites.Star, Mathf.Min(r, 1f), Mathf.Min(r, 1f), 45f, Alpha(bright, f * f));
                    break;
                }

            case ZoneStyle.Tether:
                {
                    if (sp.linkA == null || sp.linkB == null) break;
                    z.frameTimer -= Time.deltaTime;
                    if (z.frameTimer <= 0f) { z.frameTimer = 0.06f; z.frame = Random.Range(0, ProcSprites.LightningFrames); }
                    Vector2 delta = sp.linkB.Position - sp.linkA.Position;
                    float length = delta.magnitude;
                    float rot = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                    Show(z.mark, ProcSprites.Lightning[z.frame], length / 1.5f, 1f, rot, Alpha(sp.color, vis));
                    Show(z.accent, ProcSprites.Lightning[z.frame], length / 1.5f, 0.3f, rot, Alpha(bright, 0.65f * vis));
                    Vector2 pulsePos = Vector2.Lerp(-delta * 0.5f, delta * 0.5f, Mathf.Repeat(z.age * 1.5f, 1f));
                    z.detail.transform.localPosition = new Vector3(pulsePos.x, pulsePos.y, 0f);
                    Show(z.detail, ProcSprites.Star, 0.35f, 0.35f, rot, Alpha(bright, 0.85f * vis));
                    if (emit)
                    {
                        SigilVfxSystem.Instance.Emit(ProcSprites.Segments, sp.linkA.Position, Vector2.one * 0.25f, Vector2.one * 0.35f, spin, 45f, Alpha(sp.color, 0.35f), 0.2f, Vector2.zero, 6);
                        SigilVfxSystem.Instance.Emit(ProcSprites.Segments, sp.linkB.Position, Vector2.one * 0.25f, Vector2.one * 0.35f, -spin, -45f, Alpha(sp.color, 0.35f), 0.2f, Vector2.zero, 6);
                    }
                    break;
                }
        }
    }
    static void Show(SpriteRenderer sr, Sprite sprite, float scaleX, float scaleY, float rotation, Color color)
    {
        sr.sprite = sprite; sr.color = color; sr.enabled = true;
        Transform t = sr.transform;
        t.localScale = new Vector3(scaleX, scaleY, 1f);
        t.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    static Color Alpha(Color c, float a) { c.a = a; return c; }

    static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad); float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}