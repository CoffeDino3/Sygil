using UnityEngine;


public static class SigilCasts
{
    const float SeedFlightLife = 2f;
    const int LagCap = 24;
    private const int MineLagCap = 40;
    private const int BugLagCap = 40;
    private const int SpiritLagCap = 60;


    public static bool Cast(SigilCaster c, SigilInstance s)
    {
        switch (s.def.kind)
        {
            case SigilKind.Bolt: return Bolt(c, s);
            case SigilKind.Blade: return Blade(c, s);
            case SigilKind.Boomerang: return Boomerang(c, s);
            case SigilKind.Comet: return Comet(c, s);
            case SigilKind.Familiar: return Familiar(c, s);
            case SigilKind.Lance: return Lance(c, s);
            case SigilKind.Mine: return Mine(c, s);
            case SigilKind.Nova: return Nova(c, s);
            case SigilKind.Pulse: return Pulse(c, s);
            case SigilKind.Rift: return Rift(c, s);
            case SigilKind.Seed: return Seed(c, s);
            case SigilKind.Snare: return Snare(c, s);
            case SigilKind.Storm: return Storm(c, s);
            case SigilKind.Tether: return Tether(c, s);
            case SigilKind.Totem: return Totem(c, s);
            case SigilKind.Veil: return Veil(c, s);
            case SigilKind.Ward: return Ward(c, s);
            case SigilKind.MagicCircle: return MagicCircle(c, s);
            case SigilKind.Bugs: return Bugs(c, s);
            default: return false;
        }
    }

    private static bool Bolt(SigilCaster c, SigilInstance s)
    {
        Vector2 p = PosOf(c);
        int i = EnemyView.Nearest(p, s.def.range);
        if (i < 0) return false;
        FireFan(p, EnemyView.Pos[i] - p, c.CountOf(s), 9f, BaseShot(c, s));
        return true;
    }
    static bool Boomerang(SigilCaster c, SigilInstance s)
    {
        Vector2 p = PosOf(c);
        int i = EnemyView.Nearest(p, s.def.range);
        if (i < 0) return false;
        ShotSpec spec = BaseShot(c, s);
        spec.motion = ShotMotion.Boomerang;
        spec.pierce = 99;
        spec.outTime = s.def.duration * 0.45f;
        spec.lifetime = s.def.duration * 4f;
        FireFan(p, EnemyView.Pos[i] - p, c.CountOf(s), 14f, spec);
        return true;
    }

    static bool Comet(SigilCaster c, SigilInstance s)
    {
        Vector2 p = PosOf(c);
        int i = EnemyView.RandomIn(p, s.def.range);
        if (i < 0) return false;
        Vector2 aim = EnemyView.Pos[i];
        const float height = 7.5f;
        float fall = height / s.def.speed;
        float blast = c.RadiusOf(s);
        float dmg = c.DamageOf(s);
        ShotSpec spec = BaseShot(c, s);
        spec.motion = ShotMotion.Comet; spec.round = true; spec.aim = aim;
        spec.damage = 0f; spec.lifetime = fall + 1f;
        spec.burstRadius = blast; spec.burstDamage = dmg;
        spec.craterLife = s.def.duration; spec.craterTick = 0.35f; spec.craterDamage = dmg * 0.2f;
        ShotSystem.Instance.Fire(aim + Vector2.up * height, Vector2.down, spec);
        ZoneSystem.Instance.Spawn(new ZoneSpec
        {
            style = ZoneStyle.Area,
            source = (SigilKind)(-1),
            pos = aim,
            color = s.def.color,
            radius = blast,
            life = fall
        });
        return true;
    }
    static bool Lance(SigilCaster c, SigilInstance s)
    {
        Vector2 p = PosOf(c);
        if (EnemyView.Nearest(p, s.def.range) < 0) return false;
        Vector2 dir = c.Mover != null ? c.Mover.FacingDir : Vector2.up;
        ShotSpec spec = BaseShot(c, s);
        spec.motion = ShotMotion.Lance;
        spec.length = 1.5f;
        spec.speed = 0f;
        spec.pierce = 999;
        spec.radius = 0.35f;
        spec.lifetime = s.def.duration;
        ShotSystem.Instance.Fire(p, dir, spec);
        return true;
    }
    static bool Nova(SigilCaster c, SigilInstance s)
    {
        Vector2 p = PosOf(c);
        if (EnemyView.Nearest(p, 6f) < 0) return false;
        ShotSpec spec = BaseShot(c, s);
        spec.round = true; spec.lifetime = s.def.duration;
        int n = c.CountOf(s);
        float offset = Random.Range(0f, 360f);
        for (int i = 0; i < n; i++)
            ShotSystem.Instance.Fire(p, FromAngle(offset + i * 360f / n), spec);
        return true;
    }
    public static void Spirits(SigilCaster c, SigilInstance s, Vector2 where)
    {
        if (ShotSystem.Instance.Count(SigilKind.Spirit) >= SpiritLagCap) return;
        ShotSpec spec = BaseShot(c, s);
        spec.round = true; spec.homing = 240f; spec.homingDelay = 0.35f;
        spec.lifetime = s.def.duration;
        int n = c.CountOf(s);
        for (int i = 0; i < n; i++)
            ShotSystem.Instance.Fire(where, FromAngle(90f + Random.Range(-45f, 45f)), spec);
    }
    private static bool Blade(SigilCaster c, SigilInstance s)
    {
        Vector2 p = PosOf(c);
        int i = EnemyView.Nearest(p, s.def.range);
        if (i < 0) return false;
        Vector2 to = EnemyView.Pos[i] - p;
        float a = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
        s.flip = !s.flip;
        ZoneSystem.Instance.Spawn(new ZoneSpec
        {
            style = ZoneStyle.Blade,
            source = SigilKind.Blade,
            pos = p,
            followOwner = true,
            color = s.def.color,
            radius = 1f,
            life = s.def.duration,
            tickDamage = c.DamageOf(s),
            sweepFrom = s.flip ? a - 85f : a + 85f,
            sweepTo = s.flip ? a + 85f : a - 85f,
            sweepReach = 2.1f,
            sweepWidth = 85f
        });
        return true;
    }
    private static bool Pulse(SigilCaster c, SigilInstance s)
    {
        Vector2 p = PosOf(c);
        float rad = c.RadiusOf(s);
        if (EnemyView.Nearest(p, rad + 1f) < 0) return false;
        ZoneSystem.Instance.Spawn(new ZoneSpec
        {
            style = ZoneStyle.Area,
            source = SigilKind.Pulse,
            pos = p,
            followOwner = true,
            color = s.def.color,
            radius = 0.4f,
            growTo = rad,
            life = s.def.duration,
            hitOnce = true,
            tickDamage = c.DamageOf(s),
            push = 7.5f
        });
        return true;
    }


    private static bool Rift(SigilCaster c, SigilInstance s)
    {
        if (ZoneSystem.Instance.Count(SigilKind.Rift) >= LagCap) return false;
        int i = EnemyView.RandomIn(PosOf(c), s.def.range);
        if (i < 0) return false;
        ZoneSystem.Instance.Spawn(new ZoneSpec
        {
            style = ZoneStyle.Rift,
            source = SigilKind.Rift,
            pos = EnemyView.Pos[i],
            color = s.def.color,
            radius = c.RadiusOf(s),
            life = s.def.duration,
            tickEvery = 0.25f,
            tickDamage = c.DamageOf(s)
        });
        return true;
    }

    static bool Seed(SigilCaster c, SigilInstance s)
    {
        if (ZoneSystem.Instance.Count(SigilKind.Seed) >= LagCap) return false;
        int i = EnemyView.RandomIn(PosOf(c), s.def.range);
        if (i < 0) return false;
        float blast = c.RadiusOf(s);
        return ZoneSystem.Instance.TrySpawn(new ZoneSpec
        {
            style = ZoneStyle.Seed,
            source = SigilKind.Seed,
            pos = PosOf(c),
            color = s.def.color,
            radius = 0.12f,
            life = s.def.duration,
            seedTarget = EnemyView.List[i],
            seedSpeed = s.def.speed,
            seedRange = s.def.range,
            seedFlightLife = SeedFlightLife,
            burstDamage = c.DamageOf(s),
            burstRadius = blast
        });
    }

    static bool Snare(SigilCaster c, SigilInstance s)
    {
        if (ZoneSystem.Instance.Count(SigilKind.Snare) >= LagCap) return false;
        int i = EnemyView.RandomIn(PosOf(c), s.def.range);
        if (i < 0) return false;
        ZoneSystem.Instance.Spawn(new ZoneSpec
        {
            style = ZoneStyle.Area,
            source = SigilKind.Snare,
            pos = EnemyView.Pos[i],
            color = s.def.color,
            radius = c.RadiusOf(s),
            life = s.def.duration,
            slow = 0.6f,
            tickEvery = 0.5f,
            tickDamage = c.DamageOf(s)
        });
        return true;
    }

    static bool Ward(SigilCaster c, SigilInstance s)
    {
        if (ZoneSystem.Instance.Count(SigilKind.Ward) >= LagCap) return false;
        Vector2 p = PosOf(c);
        if (EnemyView.Nearest(p, 5f) < 0) return false;
        float rad = c.RadiusOf(s);
        ZoneSystem.Instance.Spawn(new ZoneSpec
        {
            style = ZoneStyle.Area,
            source = SigilKind.Ward,
            pos = p,
            followOwner = true,
            color = s.def.color,
            radius = rad,
            life = s.def.duration,
            burstDamage = c.DamageOf(s),
            burstRadius = rad * 1.8f,
            push = 15f
        });
        return true;
    }
    private static bool Storm(SigilCaster c, SigilInstance s)
    {
        if (ZoneSystem.Instance.Count(SigilKind.Storm) >= LagCap) return false;
        Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(3f, 5f);
        ZoneSystem.Instance.Spawn(new ZoneSpec
        {
            style = ZoneStyle.Cloud,
            source = SigilKind.Storm,
            pos = PosOf(c) + offset,
            color = s.def.color,
            radius = c.RadiusOf(s),
            life = s.def.duration,
            drift = Random.insideUnitCircle.normalized * 1.2f,
            strikeEvery = 0.45f,
            strikeDamage = c.DamageOf(s),
            strikeRadius = 1.1f
        });
        return true;
    }
    static bool Mine(SigilCaster c, SigilInstance s)
    {
        Vector2 p = PosOf(c);
        if (Vector2.Distance(p, s.lastDropPos) < 1.8f) return false;
        if (ZoneSystem.Instance.Count(SigilKind.Mine) >= MineLagCap) return false;
        ZoneSystem.Instance.Spawn(new ZoneSpec
        {
            style = ZoneStyle.Mine,
            source = SigilKind.Mine,
            pos = p,
            color = s.def.color,
            radius = 0.4f,
            life = s.def.duration,
            armDelay = 0.6f,
            triggerRadius = 0.8f,
            burstDamage = c.DamageOf(s),
            burstRadius = c.RadiusOf(s)
        });
        s.lastDropPos = p;
        return true;
    }
    static bool Familiar(SigilCaster c, SigilInstance s)
    {
        if (ZoneSystem.Instance.Count(SigilKind.Familiar) >= LagCap) return false;
        Vector2 p = PosOf(c);
        ShotSpec shot = BaseShot(c, s);
        int n = c.CountOf(s);
        float startAngle = Random.Range(0f, 360f);
        float dirSign = Random.value < 0.5f ? 1f : -1f;
        for (int i = 0; i < n; i++)
        {
            ZoneSystem.Instance.Spawn(new ZoneSpec
            {
                style = ZoneStyle.Familiar,
                source = SigilKind.Familiar,
                pos = p,
                color = s.def.color,
                radius = 0.35f,
                life = s.def.duration,
                orbitRadius = 1.6f,
                orbitSpeed = 115f * dirSign,
                orbitAngle = startAngle + i * 360f / n,
                shootEvery = 0.95f,
                shootRange = s.def.range,
                shot = shot
            });
        }
        return true;
    }

    static bool Totem(SigilCaster c, SigilInstance s)
    {
        if (ZoneSystem.Instance.Count(SigilKind.Totem) >= c.CountOf(s)) return false;
        ZoneSystem.Instance.Spawn(new ZoneSpec
        {
            style = ZoneStyle.Totem,
            source = SigilKind.Totem,
            pos = PosOf(c) + Random.insideUnitCircle * 1.5f,
            color = s.def.color,
            radius = 0.45f,
            life = s.def.duration,
            shootEvery = 0.85f,
            shootRange = s.def.range,
            shot = BaseShot(c, s)
        });
        return true;
    }
    static bool Veil(SigilCaster c, SigilInstance s)
    {
        Vector2 p = PosOf(c);
        if (EnemyView.Nearest(p, 7f) < 0) return false;
        if (ZoneSystem.Instance.Count(SigilKind.Veil) >= LagCap) return false;
        ZoneSystem.Instance.Spawn(new ZoneSpec
        {
            style = ZoneStyle.Decoy,
            source = SigilKind.Veil,
            pos = p + Random.insideUnitCircle * 2.2f,
            color = s.def.color,
            radius = 0.5f,
            life = s.def.duration,
            decoy = true
        });
        return true;
    }

    private static bool Tether(SigilCaster c, SigilInstance s)
    {
        if (ZoneSystem.Instance.Count(SigilKind.Tether) >= LagCap) return false;
        int a = NearestUnlinked(PosOf(c), s.def.range, -1);
        if (a < 0) return false;
        int b = NearestUnlinked(EnemyView.Pos[a], s.def.radius, a);
        if (b < 0) return false;
        ZoneSystem.Instance.Spawn(new ZoneSpec
        {
            style = ZoneStyle.Tether,
            source = SigilKind.Tether,
            pos = (EnemyView.Pos[a] + EnemyView.Pos[b]) * 0.5f,
            color = s.def.color,
            radius = 0.5f,
            life = s.def.duration,
            tickEvery = 0.5f,
            tickDamage = c.DamageOf(s),
            linkA = EnemyView.List[a],
            linkB = EnemyView.List[b]
        });
        return true;
    }

    static bool MagicCircle(SigilCaster c, SigilInstance s)
    {
        if (ZoneSystem.Instance.Count(SigilKind.MagicCircle) >= LagCap) return false;
        Vector2 p = PosOf(c);
        if (EnemyView.Nearest(p, s.def.range) < 0) return false;
        ZoneSystem.Instance.Spawn(new ZoneSpec
        {
            style = ZoneStyle.Circle,
            source = SigilKind.MagicCircle,
            pos = p,
            color = s.def.color,
            radius = c.RadiusOf(s),
            life = s.def.duration,
            auraDamage = s.def.buffDamage * s.LevelMul,
            auraCast = s.def.buffCast * s.LevelMul
        });
        return true;
    }
    private static bool Bugs(SigilCaster c, SigilInstance s)
    {
        Vector2 p = PosOf(c);
        if (EnemyView.Nearest(p, s.def.range + 4f) < 0) return false;
        int n = c.CountOf(s);
        float dmg = c.DamageOf(s);
        for (int i = 0; i < n; i++)
        {
            if (ZoneSystem.Instance.Count(SigilKind.Bugs) >= BugLagCap) break;
            ZoneSystem.Instance.Spawn(new ZoneSpec
            {
                style = ZoneStyle.Bug,
                source = SigilKind.Bugs,
                pos = p + Random.insideUnitCircle * 1.2f,
                color = s.def.color,
                radius = s.def.radius,
                life = s.def.duration,
                walkSpeed = s.def.speed,
                seekRange = s.def.range,
                leash = 7f,
                contactDamage = dmg,
                contactEvery = 0.5f,
                health = s.def.health * s.LevelMul
            });
        }
        return true;
    }
    static Vector2 PosOf(SigilCaster c) => c.transform.position;
    static Vector2 FromAngle(float deg)
    { float r = deg * Mathf.Deg2Rad; return new Vector2(Mathf.Cos(r), Mathf.Sin(r)); }
    static Vector2 Rotate(Vector2 v, float deg)
    { float r = deg * Mathf.Deg2Rad; float c = Mathf.Cos(r); float s = Mathf.Sin(r); return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c); }
    static ShotSpec BaseShot(SigilCaster c, SigilInstance s)
    {
        SigilDef d = s.def;
        return new ShotSpec
        {
            source = d.kind,
            damage = c.DamageOf(s),
            speed = d.speed,
            radius = 0.08f + d.size * 0.2f,
            size = d.size,
            lifetime = d.range * 1.5f / Mathf.Max(0.1f, d.speed),
            pierce = c.BonusPierce,
            color = d.color,
            flags = c.BonusFlags,
            motion = ShotMotion.Straight
        };
    }
    static void FireFan(Vector2 origin, Vector2 dir, int count, float spread, ShotSpec spec)
    {
        for (int i = 0; i < count; i++)
        {
            float offset = count == 1 ? 0f : Mathf.Lerp(-spread, spread, i / (float)(count - 1));
            ShotSystem.Instance.Fire(origin, Rotate(dir, offset), spec);
        }
    }

    static int NearestUnlinked(Vector2 from, float range, int skip)
    {
        EnemyView.Refresh();
        int best = -1; float bestSqr = range * range;
        for (int k = 0; k < EnemyView.Count; k++)
        {
            if (k == skip || !EnemyView.IsAlive(k)) continue;
            if (Damage.IsLinked(EnemyView.List[k])) continue;
            float sqr = (EnemyView.Pos[k] - from).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = k; }
        }
        return best;
    }
}