using UnityEngine;

public enum SigilKind
{
    Bolt, Blade, Boomerang, Comet, Familiar, Lance, Mine, Nova, Pulse, Reprise,
    Rift, Seed, Snare, Spirit, Storm, Tether, Totem, Veil, Ward,
    MagicCircle, Bugs
}

public class SigilDef
{
    public SigilKind kind;
    public string name;
    public string description;

    public float damage = 1f;
    public float cooldown = 1f;
    public float speed = 8f;
    public float radius = 1f;
    public float duration = 1f;
    public float range = 8f;
    public int count = 1;
    public float size = 0.5f;
    public Color color = Color.white;
    public float buffDamage = 0f;
    public float buffCast = 0f;
    public float health = 0f;
}

public static class SigilLibrary
{
    static readonly SigilDef[] all = Build();
    public static SigilDef Get(SigilKind kind)
    {
        return all[(int)kind];
    }
    private static SigilDef[] Build()
    {
        SigilDef[] list =
        {
            new SigilDef { kind = SigilKind.Bolt, name = "Bolt",
                description = "Fires a bolt at the nearest enemy",
                damage=1f, cooldown = 0.8f, speed = 10f, range = 8f, size = 0.5f,
                color = new Color(0.45f, 0.9f, 1f) },
            new SigilDef { kind = SigilKind.Blade, name = "Blade",
                description = "A conjured blade slashes in an arc around you",
                damage=2f, cooldown = 1.4f, radius = 2f, duration = 0.28f, range = 3.2f,
                color = new Color(1f, 0.95f, 0.8f) },
            new SigilDef { kind = SigilKind.Boomerang, name = "Boomerang",
                description = "Flies out and returns, hitting both ways",
                damage=1.5f, cooldown = 2.2f, speed = 9f, duration = 0.7f, range = 8f, size = 0.8f,
                color = new Color(1f, 0.82f, 0.48f) },

            new SigilDef { kind = SigilKind.Comet, name = "Comet",
                description = "A heavy comet crashes down and leaves a crater",
                damage = 4f, cooldown = 7f, speed = 6f, radius = 1.8f, duration = 3f, range = 9f, size = 1.3f,
                color = new Color(1f, 0.55f, 0.3f) },
            new SigilDef { kind = SigilKind.Familiar, name = "Familiar",
                description = "A companion floats beside you and fights",
                damage=1f, cooldown = 15f, speed = 9f, duration = 14f, range = 7f, count = 1, size = 0.4f,
                color = new Color(0.56f, 0.89f, 0.75f) },
            new SigilDef { kind = SigilKind.Lance, name = "Lance",
                description = "A long piercing thrust where you're heading",
                damage = 3f, cooldown = 2.5f, radius = 4f, duration = 0.18f, range = 6f, size = 0.5f,
                color = new Color(0.8f, 0.7f, 1f) },
            new SigilDef { kind = SigilKind.Mine, name = "Mine",
                description = "Drops hidden explosives as you move",
                damage = 3.5f, cooldown = 2f, radius = 1.6f, duration = 25f, count = 8,
                color = new Color(1f, 0.54f, 0.48f) },
            new SigilDef { kind = SigilKind.Nova, name = "Nova",
                description = "A ring of bolts shoots out in every direction",
                damage = 1f, cooldown = 4f, speed = 7f, duration = 1.1f, count = 10, size = 0.4f,
                color=new Color(0.72f, 0.61f, 1f) },
            new SigilDef { kind = SigilKind.Pulse, name = "Pulse",
                description = "A burst of force centered on you",
                damage = 2f, cooldown = 3f, radius = 2.6f, duration = 0.35f,
                color = new Color(1f, 0.9f, 0.6f) },
            new SigilDef { kind = SigilKind.Reprise, name = "Reprise",
                description = "Repeats the last sigil you cast",
                cooldown = 3f,
                color = Color.white },

            new SigilDef { kind = SigilKind.Rift, name = "Rift",
                description = "Tears the ground open, hurting anything inside",
                damage = 1f, cooldown = 6f, radius = 1.5f, duration = 4.5f, range = 8f,
                color=new Color(0.7f, 0.4f, 1f) },

            new SigilDef { kind = SigilKind.Seed, name = "Seed",
                description = "Launches a seed that sticks to an enemy, then explodes. Detonates early if its host dies.",
                damage = 4f, cooldown = 4f, radius = 1.5f, duration = 1.6f, range = 8f, speed = 12f,
                color = new Color(0.5f, 0.9f, 0.4f) },

            new SigilDef { kind = SigilKind.Snare, name = "Snare",
                description = "A sticky zone that slows enemies",
                damage = 0.3f, cooldown = 7f, radius = 1.7f, duration = 6f, range = 8f,
                color = new Color(0.4f, 0.9f, 0.8f) },
            new SigilDef { kind = SigilKind.Spirit, name = "Spirit",
                description = "Fallen enemies release homing spirits",
                damage = 1.2f, speed = 5.5f, duration = 2.5f, count = 1, size = 0.45f,
                color = new Color(0.85f, 1f, 0.9f) },
            new SigilDef { kind = SigilKind.Storm, name = "Storm",
                description = "A roaming cloud that strikes at random",
                damage = 3f, cooldown = 12f, radius = 2.2f, duration = 8f,
                color = new Color(0.55f, 0.65f, 1f) },
            new SigilDef { kind = SigilKind.Tether, name = "Tether",
                description = "Links two enemies so they share damage",
                damage = 0.4f, cooldown = 8f, radius = 4f, duration = 6f, range = 8f,
                color = new Color(1f, 0.6f, 0.9f) },

            new SigilDef { kind = SigilKind.Totem, name = "Totem",
                description = "A stationary turret that shoots nearby foes",
                damage=1f, cooldown = 10f, speed = 9f, duration = 9f, range = 7f, count = 2, size = 0.45f,
                color = new Color(1f, 0.78f, 0.4f) },
            new SigilDef { kind = SigilKind.Veil, name = "Veil",
                description = "Leaves decoys that distract enemies",
                cooldown = 9f, duration = 5f, count = 2,
                color = new Color(0.75f, 0.65f, 1f) },

            new SigilDef { kind = SigilKind.Ward, name = "Ward",
                description = "A barrier that pushes enemies back",
                damage = 3.5f, cooldown = 8f, radius = 2.2f, duration = 4.5f,
                color = new Color(0.55f, 0.8f, 1f) },

            new SigilDef { kind = SigilKind.Bugs, name = "Bugs",
                description = "Summons tiny critters that scuttle around and bite whatever they touch. Enemies can squash them.",
                damage = 0.8f, cooldown = 12f, speed = 3.2f, radius = 0.25f, duration = 10f, range = 6f, count = 3, health = 5f,
                color=new Color(0.78f, 0.9f, 0.35f) },
            new SigilDef { kind = SigilKind.MagicCircle, name = "Magic Circle",
                description = "A circle at your feet: stand inside for extra damage and faster casting",
                cooldown = 12f, radius = 2.2f, duration = 8f, range = 9f,
                buffDamage=0.25f, buffCast = 0.25f,
                color = new Color(1f, 0.85f, 0.45f) },
        };
        SigilDef[] byKind = new SigilDef[System.Enum.GetValues(typeof(SigilKind)).Length];
        foreach (SigilDef d in list) byKind[(int)d.kind] = d;
        return byKind;
    }
}


public class SigilInstance
{
    public const int MaxLevel = 5;

    public readonly SigilDef def;
    public int level = 1;
    public float timer;
    public Vector2 lastDropPos;
    public bool flip;
    public SigilInstance(SigilDef def)
    {
        this.def = def;
    }

    public float LevelMul => 1f + 0.25f * (level - 1);
}