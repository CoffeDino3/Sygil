using System;
using UnityEngine;
[Flags]
public enum ShotFlags
{
    None = 0,
    Crackle = 1 << 0,
}


public enum ShotMotion
{
    Straight,
    Boomerang,
    Lance,
    Comet,
}

public struct ShotSpec
{
    public SigilKind source;
    public float damage;
    public float speed;
    public float radius;
    public float size;
    public float lifetime;
    public int pierce;
    public Color color;
    public ShotFlags flags;
    public bool round;


    public ShotMotion motion;
    public float length;
    public float outTime;
    public float homing;
    public float homingDelay;
    public Vector2 aim;
    public float burstRadius;
    public float burstDamage;
    public float craterLife;
    public float craterTick;
    public float craterDamage;
}