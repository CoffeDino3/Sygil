using System.Collections.Generic;
using UnityEngine;
public static class ProcSprites
{
    public const int LightningFrames = 6;

    public static Sprite Bolt;
    public static Sprite Orb;
    public static Sprite Disc;
    public static Sprite Ring;
    public static Sprite Arc;
    public static Sprite Diamond;
    public static Sprite DiamondFrame;
    public static Sprite Bar;
    public static Sprite Rift;
    public static readonly Sprite[] Lightning = new Sprite[LightningFrames];

    public static Sprite Sabre;
    public static Sprite Lance;
    public static Sprite Boomerang;
    public static Sprite Cloud;
    public static Sprite Chasm;
    public static Sprite SnareBrambles;
    public static Sprite TotemPillar;
    public static Sprite FamiliarCritter;
    public static Sprite DecoyPhantom;
    public static Sprite AegisBarrier;
    public static Sprite BugCritter;
    public static Sprite MagicCircleSigil;
    public static Sprite Runes, Segments, Star, Wisp, Crescent, SeedBody;
    public static Material GlowMaterial { get; set; }
    private static readonly List<Sprite> ownedSprites = new List<Sprite>();
    static Material material;
    static ProcSprites()
    {
        Ensure();
    }
    public static void Release()
    {
        foreach (Sprite sprite in ownedSprites)
        {
            if (sprite == null) continue;
            if (sprite.texture != null) Object.Destroy(sprite.texture);
            Object.Destroy(sprite);
        }
        ownedSprites.Clear();
        if (material != null) Object.Destroy(material);
        material = null;
        GlowMaterial = null;
        Bolt = Orb = Disc = Ring = Arc = Rift = Diamond = DiamondFrame = Bar = null;
        Sabre = Lance = Boomerang = Cloud = Chasm = SnareBrambles = TotemPillar = FamiliarCritter = DecoyPhantom = AegisBarrier = BugCritter = MagicCircleSigil = null;
        Runes = Segments = Star = Wisp = Crescent = SeedBody = null;
        for (int i = 0; i < LightningFrames; i++) Lightning[i] = null;
    }

    public static Material Material
    {
        get
        {
            if (material == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null) material = new Material(shader);
            }
            return material;
        }
    }

    public static void Ensure()
    {
        if (Bolt != null && Sabre != null && Lance != null && Cloud != null) return;
        Bolt = MakeBolt();
        Orb = MakeOrb();
        Disc = MakeDisc();
        Ring = MakeRing();
        Arc = MakeArc();
        Diamond = MakeDiamond();
        DiamondFrame = MakeDiamondFrame();
        Bar = MakeBar();
        Rift = MakeRift();

        Sabre = MakeSabre();
        Lance = MakeLance();
        Boomerang = MakeBoomerang();
        Cloud = MakeCloud();
        Chasm = MakeChasm();
        SnareBrambles = MakeSnareBrambles();
        TotemPillar = MakeTotemPillar();
        FamiliarCritter = MakeFamiliarCritter();
        DecoyPhantom = MakeDecoyPhantom();
        AegisBarrier = MakeAegisBarrier();
        BugCritter = MakeBugCritter();
        MagicCircleSigil = MakeMagicCircleSigil();


        Runes = Make(192, 192, 96f, (x, y) =>
        {
            float dx = x - 96f, dy = y - 96f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Atan2(dy, dx);
            float sector = Mathf.Repeat(a + Mathf.PI * 2f, Mathf.PI / 6f) - Mathf.PI / 12f;
            float ring = 1.3f - Mathf.Abs(r - 90f);
            float inner = 0.9f - Mathf.Abs(r - 72f);
            float glyph = Mathf.Abs(sector) < 0.035f && r > 76f && r < 87f ? 1f : 0f;
            float diamond = 1.1f - Mathf.Abs(Mathf.Abs(sector) * 80f + Mathf.Abs(r - 81f) - 5f);
            return Mathf.Max(ring, inner * 0.65f, glyph, diamond);
        });

        Segments = Make(128, 128, 64f, (x, y) =>
        {
            float dx = x - 64f, dy = y - 64f;
            float a = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 360f, 60f);
            return a < 42f ? 1.5f - Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - 60f) : 0f;
        });
        Star = Make(64, 64, 64f, (x, y) =>
        {
            float dx = Mathf.Abs(x - 32f), dy = Mathf.Abs(y - 32f);
            return Mathf.Pow(Mathf.Clamp01(1f - Mathf.Min(dx, dy) / 3f), 2f) * Mathf.Clamp01(1f - Mathf.Max(dx, dy) / 30f);
        });

        Wisp = Make(96, 32, 64f, (x, y) => {
            float width = 1f + x * 0.09f;
            float bend = Mathf.Sin(x / 96f * Mathf.PI) * 3f;
            return Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(y - 16f - bend) / width), 2f) * Mathf.Sin(x / 96f * Mathf.PI);
        });
        Crescent = Make(64, 64, 64f, (x, y) =>
        {
            float outer = 0.8f - (Vector2.Distance(new Vector2(x, y), new Vector2(32f, 32f)) - 27f);
            float cut = Vector2.Distance(new Vector2(x, y), new Vector2(42f, 36f)) - 24f;
            return Mathf.Min(outer, cut);
        });

        SeedBody = Make(64, 64, 64f, (x, y) =>
        {
            float cx = 32f, cy = 32f;
            float dx = x - cx, dy = y - cy;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx);

            float spikes = 1f + Mathf.Sin(angle * 5f) * 0.22f + Mathf.Sin(angle * 8f) * 0.12f;
            float shellR = 17f * spikes;
            float shell = Mathf.Clamp01((shellR - r) / 2.5f);
            float core = Mathf.Pow(Mathf.Clamp01(1f - r / 7f), 1.8f);
            float veinAngle = Mathf.Repeat(angle * Mathf.Rad2Deg + 360f, 72f);
            float vein = (veinAngle < 5f && r > 6f && r < shellR - 1f)
                ? Mathf.Clamp01(1f - veinAngle / 5f) * 0.65f : 0f;

            float membrane = (r > shellR - 4f && r < shellR) ? 0.35f : 0f;

            return Mathf.Max(shell * 0.75f, core, vein, membrane);
        });
        for (int i = 0; i < LightningFrames; i++)
            Lightning[i] = MakeLightning(1000 + i);
    }
    static Sprite MakeSabre()
    {
        const int w = 160, h = 48;
        return Make(w, h, 64f, (x, y) => {
            if (x >= 4f && x <= 12f)
            {
                float dy = Mathf.Abs(y - 24f);
                return 0.5f - (Mathf.Abs(x - 8f) + dy - 6f);
            }
            if (x > 12f && x <= 36f)
            {
                float gripDy = Mathf.Abs(y - 24f);
                float grip = 0.5f - (gripDy - 2.8f);
                float bowY = 24f - Mathf.Sin((x - 12f) / 24f * Mathf.PI) * 14f;
                float bow = 0.5f - (Mathf.Abs(y - bowY) - 2.0f);
                return Mathf.Max(grip, bow);
            }
            if (x > 36f && x <= 156f)
            {
                float nx = (x - 36f) / 120f;
                float curveY = 24f + Mathf.Pow(nx, 1.6f) * 10f;
                float dy = y - curveY;
                float halfThick = Mathf.Lerp(5.0f, 0.5f, nx);
                if (dy < -halfThick || dy > halfThick + 1f) return 0f;
                float core = 0.5f - (Mathf.Abs(dy) - halfThick);
                float edgeGleam = Mathf.Clamp01(1f - Mathf.Abs(dy + halfThick) / 2f);
                float fuller = (dy > 0.5f && dy < halfThick * 0.7f && nx < 0.75f) ? 0.45f : 1f;
                return Mathf.Clamp01(core * fuller + edgeGleam * 0.6f);
            }
            return 0f;
        }, new Vector2(24f / 160f, 0.5f));
    }

    private static Sprite MakeLance()
    {
        const int w = 192, h = 40;
        return Make(w, h, 64f, (x, y) => {
            float dy = Mathf.Abs(y - 20f);
            if (x >= 6f && x <= 34f) return 0.5f - (dy - 2.8f);
            if (x > 32f && x <= 46f)
            {
                float t = (x - 32f) / 14f;
                float guardHalf = Mathf.Lerp(3f, 15f, t);
                return 0.5f - (dy - guardHalf);
            }
            if (x > 46f && x <= 148f)
            {
                float nx = (x - 46f) / 102f;
                float bodyThick = Mathf.Lerp(6.0f, 3.0f, nx);
                float shaft = 0.5f - (dy - bodyThick);
                float flute = Mathf.Sin(x * 0.3f) * 0.15f;
                return shaft + flute;
            }
            if (x > 148f && x <= 188f)
            {
                float nx = (x - 148f) / 40f;
                float headThick = nx < 0.35f ? Mathf.Lerp(3.0f, 8.0f, nx / 0.35f) : Mathf.Lerp(8.0f, 0.4f, (nx - 0.35f) / 0.65f);
                return 0.5f - (dy - headThick);
            }
            return 0f;
        }, new Vector2(28f / 192f, 0.5f));
    }


    static Sprite MakeBoomerang()
    {
        const int s = 80;
        Vector2 elbow = new Vector2(40f, 22f);
        Vector2 leftTip = new Vector2(16f, 62f);
        Vector2 rightTip = new Vector2(64f, 62f);
        return Make(s, s, 64f, (x, y) =>
        {
            Vector2 p = new Vector2(x, y);
            float d = Mathf.Min(DistToSegment(p, elbow, leftTip), DistToSegment(p, elbow, rightTip));
            float t = Mathf.Clamp01((y - 22f) / 40f);
            float halfThick = Mathf.Lerp(7.0f, 2.5f, t);
            return 0.5f - (d - halfThick);
        });
    }
    static Sprite MakeCloud()
    {
        const int s = 128;
        Vector2[] lobes = { new Vector2(64f,60f), new Vector2(44f,54f), new Vector2(84f,54f),
                            new Vector2(52f,76f), new Vector2(76f,76f), new Vector2(64f,40f) };
        float[] radii = { 34f, 26f, 26f, 24f, 24f, 22f };
        return Make(s, s, 64f, (x, y) => {
            Vector2 p = new Vector2(x, y);
            float cloud = 0f;
            for (int i = 0; i < lobes.Length; i++)
                cloud = Mathf.Max(cloud, Mathf.Clamp01(1f - (p - lobes[i]).magnitude / radii[i]));
            return Mathf.Pow(cloud, 1.8f);
        });
    }
    static Sprite MakeChasm()
    {
        const int s = 128;
        System.Random rng = new System.Random(42);
        List<Vector4> veins = new List<Vector4>();
        Vector2 prev = new Vector2(12f, 64f);
        for (int i = 1; i <= 8; i++)
        {
            float nx = Mathf.Lerp(12f, 116f, i / 8f);
            float ny = (i == 8) ? 64f : 64f + ((float)rng.NextDouble() * 2f - 1f) * 22f;
            Vector2 curr = new Vector2(nx, ny);
            veins.Add(new Vector4(prev.x, prev.y, curr.x, curr.y));
            if (i > 1 && i < 8 && rng.NextDouble() < 0.5)
            {
                float bLen = 14f + (float)rng.NextDouble() * 16f;
                float bAng = ((float)rng.NextDouble() * 2f - 1f) * 1.2f;
                Vector2 bEnd = curr + new Vector2(Mathf.Sin(bAng), Mathf.Cos(bAng)) * bLen;
                veins.Add(new Vector4(curr.x, curr.y, bEnd.x, bEnd.y));
            }
            prev = curr;
        }
        return Make(s, s, 64f, (x, y) =>
        {
            Vector2 pt = new Vector2(x, y);
            float minD = float.MaxValue;
            for (int k = 0; k < veins.Count; k++) minD = Mathf.Min(minD, DistToSegment(pt, veins[k]));
            return (0.5f - (minD - 2.8f)) + Mathf.Pow(Mathf.Clamp01(1f - minD / 9f), 2f) * 0.65f;
        });
    }
    private static Sprite MakeSnareBrambles()
    {
        const int s = 128;
        List<Vector4> strands = new List<Vector4>();
        for (int i = 0; i < 8; i++)
        {
            float a1 = (i * 45f) * Mathf.Deg2Rad;
            float a2 = ((i + 3) * 45f) * Mathf.Deg2Rad;
            Vector2 p1 = new Vector2(64f + Mathf.Cos(a1) * 52f, 64f + Mathf.Sin(a1) * 52f);
            Vector2 p2 = new Vector2(64f + Mathf.Cos(a2) * 52f, 64f + Mathf.Sin(a2) * 52f);
            strands.Add(new Vector4(p1.x, p1.y, p2.x, p2.y));
        }
        return Make(s, s, 64f, (x, y) =>
        {
            Vector2 pt = new Vector2(x, y);
            float minD = float.MaxValue;
            for (int k = 0; k < strands.Count; k++) minD = Mathf.Min(minD, DistToSegment(pt, strands[k]));
            return 0.5f - (minD - 2.2f);
        });
    }
    private static Sprite MakeTotemPillar()
    {
        const int w = 48, h = 112;
        return Make(w, h, 64f, (x, y) => {
            float dx = Mathf.Abs(x - 24f);
            if (y >= 6f && y <= 24f) return 0.5f - (dx - (y < 14f ? 20f : 16f));
            if (y > 24f && y <= 96f)
            {
                float body = 0.5f - (dx - Mathf.Lerp(14f, 10f, (y - 24f) / 72f));
                return body * ((dx < 2.5f) ? 0.35f : 1.0f);
            }
            if (y > 96f && y <= 108f) return 0.5f - (dx - Mathf.Lerp(10f, 1f, (y - 96f) / 12f));
            return 0f;
        });
    }
    private static Sprite MakeFamiliarCritter()
    {
        const int s = 80;
        return Make(s, s, 64f, (x, y) => {
            float dx = x - 40f, dy = y - 40f;
            float head = 0.5f - (Mathf.Sqrt(dx * dx + (dy - 6f) * (dy - 6f)) - 10f);
            float wingL = 0.5f - (Mathf.Sqrt((dx + 16f) * (dx + 16f) + (dy + 2f) * (dy + 2f) * 2f) - 14f);
            float wingR = 0.5f - (Mathf.Sqrt((dx - 16f) * (dx - 16f) + (dy + 2f) * (dy + 2f) * 2f) - 14f);
            float tail = dy < -6f && Mathf.Abs(dx) < Mathf.Lerp(6f, 0.5f, (-dy - 6f) / 24f) ? 0.8f : 0f;
            float eyeMask = Mathf.Sqrt((dx - 3f) * (dx - 3f) + (dy - 8f) * (dy - 8f)) < 2.5f ? 0f : 1f;
            return Mathf.Max(head, wingL, wingR, tail) * eyeMask;
        });
    }
    private static Sprite MakeDecoyPhantom()
    {
        const int w = 64, h = 96;
        return Make(w, h, 64f, (x, y) =>
        {
            float dx = Mathf.Abs(x - 32f);
            if (y >= 64f && y <= 90f)
            {
                float hood = 0.5f - (Mathf.Sqrt(dx * dx + (y - 74f) * (y - 74f)) - 14f);
                if (Mathf.Abs(y - 74f) < 2f && dx > 2f && dx < 6f) return 0f;
                return hood;
            }
            if (y >= 8f && y < 64f)
            {
                float cloak = 0.5f - (dx - Mathf.Lerp(24f, 12f, (y - 8f) / 56f));
                return cloak + ((y < 16f) ? Mathf.Sin(x * 0.8f) * 0.2f : 0f);
            }
            return 0f;
        });
    }
    static Sprite MakeAegisBarrier()
    {
        const int s = 128;
        return Make(s, s, 64f, (x, y) =>
        {
            float dx = x - 64f, dy = y - 64f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 360f, 45f);
            float plateRing = 0.5f - (Mathf.Abs(r - 56f) - 3.2f);
            float ribs = a < 4f && r > 44f && r < 60f ? 0.9f : 0f;
            float inner = r < 54f ? Mathf.Pow(Mathf.Clamp01(r / 54f), 3f) * 0.25f : 0f;
            return Mathf.Max(plateRing, ribs, inner);
        });
    }
    static Sprite MakeBugCritter()
    {
        return Make(96, 64, 64f, (x, y) =>
        {
            Vector2 p = new Vector2(x, y);
            float abdomen = Ellipse(p, 36f, 32f, 21f, 14f);
            float thorax = Ellipse(p, 57f, 32f, 10f, 10f);
            float head = Ellipse(p, 72f, 32f, 8f, 7.5f);
            float body = Mathf.Max(abdomen, Mathf.Max(thorax, head));
            float cut = 0f;
            if (x > 16f && x < 52f && Mathf.Abs(y - 32f) < 0.9f) cut = Mathf.Max(cut, 0.6f);
            if (Mathf.Abs(x - 47f) < 0.9f && Mathf.Abs(y - 32f) < 12f) cut = Mathf.Max(cut, 0.45f);
            if (Mathf.Abs(x - 65f) < 0.8f && Mathf.Abs(y - 32f) < 8f) cut = Mathf.Max(cut, 0.45f);
            if (Vector2.Distance(p, new Vector2(30f, 39f)) < 3.2f) cut = Mathf.Max(cut, 0.5f);
            if (Vector2.Distance(p, new Vector2(30f, 25f)) < 3.2f) cut = Mathf.Max(cut, 0.5f);
            if (Vector2.Distance(p, new Vector2(40f, 35f)) < 2.4f) cut = Mathf.Max(cut, 0.5f);
            if (Vector2.Distance(p, new Vector2(40f, 29f)) < 2.4f) cut = Mathf.Max(cut, 0.5f);
            if (Vector2.Distance(p, new Vector2(75f, 37f)) < 1.9f) cut = 1f;
            if (Vector2.Distance(p, new Vector2(75f, 27f)) < 1.9f) cut = 1f;
            body *= 1f - cut;

            float legs = 0f;
            for (int s = -1; s <= 1; s += 2)
            {
                legs = Mathf.Max(legs, Leg(p, new Vector2(56f, 32f + s * 7f), new Vector2(62f, 32f + s * 14f), new Vector2(70f, 32f + s * 22f)));
                legs = Mathf.Max(legs, Leg(p, new Vector2(46f, 32f + s * 10f), new Vector2(46f, 32f + s * 19f), new Vector2(43f, 32f + s * 27f)));
                legs = Mathf.Max(legs, Leg(p, new Vector2(36f, 32f + s * 11f), new Vector2(30f, 32f + s * 18f), new Vector2(20f, 32f + s * 24f)));
            }
            float ant = 0f;
            for (int s = -1; s <= 1; s += 2)
            {
                ant = Mathf.Max(ant, Stroke(p, new Vector2(79f, 32f + s * 3f), new Vector2(87f, 32f + s * 10f), 1.1f));
                ant = Mathf.Max(ant, Stroke(p, new Vector2(87f, 32f + s * 10f), new Vector2(92f, 32f + s * 9f), 1.1f));
            }
            return Mathf.Max(body, Mathf.Max(legs * 0.8f, ant * 0.8f));
        });
    }

    static Sprite MakeMagicCircleSigil()
    {
        const int size = 256;
        Vector2 c = new Vector2(128f, 128f);
        Vector2[] up = new Vector2[3];
        Vector2[] down = new Vector2[3];
        for (int i = 0; i < 3; i++)
        {
            float a1 = (90f + 120f * i) * Mathf.Deg2Rad;
            float a2 = (270f + 120f * i) * Mathf.Deg2Rad;
            up[i] = c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * 98f;
            down[i] = c + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * 98f;
        }
        return Make(size, size, 128f, (x, y) =>
        {
            Vector2 p = new Vector2(x, y);
            float dx = x - 128f, dy = y - 128f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float ang = Mathf.Atan2(dy, dx);

            float outer = 0.5f - (Mathf.Abs(r - 121f) - 2.6f);
            float second = 0.5f - (Mathf.Abs(r - 113f) - 0.9f);
            float mid = 0.5f - (Mathf.Abs(r - 100f) - 1.2f);
            float inner = 0.5f - (Mathf.Abs(r - 72f) - 2f);
            float core = 0.5f - (Mathf.Abs(r - 16f) - 1.8f);
            float centre = 0.5f - (r - 4.5f);

            float step = Mathf.PI * 2f / 36f;
            float local = Mathf.Repeat(ang + step * 0.5f, step) - step * 0.5f;
            float ticks = (r > 103f && r < 110f) ? 0.5f - (Mathf.Abs(local) * r - 1.2f) : 0f;

            float stepR = Mathf.PI * 2f / 12f;
            float localR = Mathf.Repeat(ang + stepR * 0.5f, stepR) - stepR * 0.5f;
            float rays = (r > 28f && r < 44f) ? 0.5f - (Mathf.Abs(localR) * r - 1f) : 0f;
            float star = 0f, dots = 0f;
            for (int i = 0; i < 3; i++)
            {
                star = Mathf.Max(star, Stroke(p, up[i], up[(i + 1) % 3], 1.5f));
                star = Mathf.Max(star, Stroke(p, down[i], down[(i + 1) % 3], 1.5f));
                dots = Mathf.Max(dots, 0.5f - (Vector2.Distance(p, up[i]) - 5f));
                dots = Mathf.Max(dots, 0.5f - (Vector2.Distance(p, down[i]) - 5f));
            }
            float a = Mathf.Max(outer, second * 0.8f);
            a = Mathf.Max(a, mid * 0.6f);
            a = Mathf.Max(a, inner * 0.9f);
            a = Mathf.Max(a, Mathf.Max(core, centre));
            a = Mathf.Max(a, ticks * 0.85f);
            a = Mathf.Max(a, rays * 0.7f);
            a = Mathf.Max(a, star * 0.9f);
            return Mathf.Max(a, dots);
        });
    }


    static float Ellipse(Vector2 p, float cx, float cy, float rx, float ry)
    {
        float nx = (p.x - cx) / rx, ny = (p.y - cy) / ry;
        return 0.5f - (Mathf.Sqrt(nx * nx + ny * ny) - 1f) * Mathf.Min(rx, ry);
    }
    static float Stroke(Vector2 p, Vector2 a, Vector2 b, float half)
    {
        return 0.5f - (DistToSegment(p, a, b) - half);
    }
    static float Leg(Vector2 p, Vector2 a, Vector2 knee, Vector2 foot)
    {
        return Mathf.Max(Stroke(p, a, knee, 1.5f), Stroke(p, knee, foot, 1.2f));
    }

    private static Sprite MakeBolt()
    {
        Vector2 a = new Vector2(16f, 12f), b = new Vector2(48f, 12f);
        return Make(64, 24, 64f, (x, y) => {
            float d = DistToSegment(new Vector2(x, y), a, b);
            return (0.5f - (d - 4.5f)) + Mathf.Pow(Mathf.Clamp01(1f - d / 11.5f), 2f) * 0.55f;
        });
    }

    static Sprite MakeOrb()
    {
        return Make(48, 48, 64f, (x, y) =>
        {
            float d = Mathf.Sqrt((x - 24f) * (x - 24f) + (y - 24f) * (y - 24f));
            return (0.5f - (d - 8f)) + Mathf.Pow(Mathf.Clamp01(1f - d / 22f), 2f) * 0.6f;
        });
    }

    static Sprite MakeDisc()
    {
        return Make(128, 128, 64f, (x, y) => 0.5f - (Mathf.Sqrt((x - 64f) * (x - 64f) + (y - 64f) * (y - 64f)) - 62.5f));
    }

    static Sprite MakeRing()
    {
        return Make(128, 128, 64f, (x, y) => {
            float d = Mathf.Sqrt((x - 64f) * (x - 64f) + (y - 64f) * (y - 64f));
            return 0.5f - (Mathf.Abs(d - 61f) - 2.5f);
        });
    }

    static Sprite MakeArc()
    {
        const float halfWidth = 45f;
        return Make(128, 128, 64f, (x, y) => {
            float dx = x - 64f, dy = y - 64f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Abs(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
            if (a > halfWidth) return 0f;
            float k = a / halfWidth;
            float thick = 7.5f * (1f - k * k);
            return Mathf.Min(0.5f - (Mathf.Abs(d - 54f) - thick), Mathf.Clamp01((halfWidth - a) / 3f));
        });
    }

    static Sprite MakeDiamond()
    {
        return Make(64, 64, 64f, (x, y) => (1f - (Mathf.Abs(x - 32f) / 24f + Mathf.Abs(y - 32f) / 24f)) * 14f);
    }
    private static Sprite MakeDiamondFrame()
    {
        return Make(96, 96, 64f, (x, y) =>
        {
            float k = Mathf.Abs(x - 48f) / 40f + Mathf.Abs(y - 48f) / 40f;
            return 0.5f - (Mathf.Abs(k - 1f) * 40f - 2.5f);
        });
    }
    static Sprite MakeBar()
    {
        return Make(64, 16, 64f, (x, y) => 0.5f - (Mathf.Abs(y - 8f) - 4f));
    }
    static Sprite MakeRift()
    {
        const int s = 128;
        System.Random rng = new System.Random(77);
        List<Vector4> mainSegs = new List<Vector4>();
        List<Vector4> tendrils = new List<Vector4>();
        Vector2 prev = new Vector2(64f, 8f);
        for (int i = 1; i <= 10; i++)
        {
            float ny = Mathf.Lerp(8f, 120f, i / 10f);
            float nx = (i == 10) ? 64f : 64f + ((float)rng.NextDouble() * 2f - 1f) * 12f;
            Vector2 curr = new Vector2(nx, ny);
            mainSegs.Add(new Vector4(prev.x, prev.y, curr.x, curr.y));

            if (i > 1 && i < 10 && rng.NextDouble() < 0.55)
            {
                float bLen = 10f + (float)rng.NextDouble() * 20f;
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                float bAng = side * (0.8f + (float)rng.NextDouble() * 0.6f);
                Vector2 bEnd = curr + new Vector2(Mathf.Cos(bAng), Mathf.Sin(bAng) * 0.4f) * bLen;
                tendrils.Add(new Vector4(curr.x, curr.y, bEnd.x, bEnd.y));
            }
            prev = curr;
        }

        return Make(s, s, 64f, (x, y) =>
        {
            Vector2 pt = new Vector2(x, y);
            float mainD = float.MaxValue;
            for (int k = 0; k < mainSegs.Count; k++)
                mainD = Mathf.Min(mainD, DistToSegment(pt, mainSegs[k]));

            float tendrilD = float.MaxValue;
            for (int k = 0; k < tendrils.Count; k++)
                tendrilD = Mathf.Min(tendrilD, DistToSegment(pt, tendrils[k]));
            float core = Mathf.Pow(Mathf.Clamp01(1f - mainD / 4.5f), 1.3f);
            float edgeGlow = Mathf.Pow(Mathf.Clamp01(1f - mainD / 14f), 2.8f) * 0.6f;
            float tendril = Mathf.Pow(Mathf.Clamp01(1f - tendrilD / 5f), 2f) * 0.5f;
            return Mathf.Max(core, edgeGlow, tendril);
        });
    }
    static Sprite MakeLightning(int seed)
    {
        const int w = 96, h = 32, points = 7;
        System.Random rng = new System.Random(seed);
        List<Vector4> segs = new List<Vector4>();
        Vector2 prev = new Vector2(4f, 16f);
        for (int i = 1; i < points; i++)
        {
            Vector2 p = new Vector2(Mathf.Lerp(4f, w - 4f, i / (float)(points - 1)),
                (i == points - 1) ? 16f : 16f + ((float)rng.NextDouble() * 2f - 1f) * 9f);
            segs.Add(new Vector4(prev.x, prev.y, p.x, p.y));
            prev = p;
        }
        return Make(w, h, 64f, (x, y) => {
            Vector2 pt = new Vector2(x, y);
            float d = float.MaxValue;
            for (int k = 0; k < segs.Count; k++) d = Mathf.Min(d, DistToSegment(pt, segs[k]));
            return (0.5f - (d - 1.2f)) + Mathf.Pow(Mathf.Clamp01(1f - d / 5f), 2f) * 0.35f;
        });
    }

    private static Sprite Make(int w, int h, float ppu, System.Func<float, float, float> alphaAt, Vector2? pivot = null)
    {
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alphaAt(x + 0.5f, y + 0.5f)) * 255f));
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(px);
        tex.Apply();
        Vector2 piv = pivot ?? new Vector2(0.5f, 0.5f);
        Sprite sp = Sprite.Create(tex, new Rect(0f, 0f, w, h), piv, ppu);
        ownedSprites.Add(sp);
        return sp;
    }
    public static float DistToSegment(Vector2 p, Vector4 seg) => DistToSegment(p, new Vector2(seg.x, seg.y), new Vector2(seg.z, seg.w));
    public static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
        return Vector2.Distance(p, a + ab * t);
    }
}