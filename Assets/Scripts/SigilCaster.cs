using System;
using System.Collections.Generic;
using UnityEngine;
public class SigilCaster : MonoBehaviour
{
    [Header("Sigils you start with (add more here to test them)")]
    [SerializeField] private SigilKind[] startingSigils = { SigilKind.Bolt };
    [SerializeField] int maxSigils = 6;
    public float DamageMultiplier = 1f;
    public float CooldownMultiplier = 1f;
    public int BonusPierce = 0;
    public ShotFlags BonusFlags = ShotFlags.None;

    public readonly List<SigilInstance> Owned = new List<SigilInstance>();
    public event Action OnSigilsChanged;

    public int MaxSigils => maxSigils;
    public PlayerController Mover { get; private set; }
    SigilInstance lastCast;


    private void Awake()
    {
        Mover = GetComponent<PlayerController>();
        ShotSystem.Instance.Owner = transform;
        ZoneSystem.Instance.Owner = transform;

        foreach (SigilKind kind in startingSigils)
            Learn(kind);
    }

    private void OnEnable() { EnemyEvents.Died += HandleEnemyDied; }
    private void OnDisable() { EnemyEvents.Died -= HandleEnemyDied; }
    public SigilInstance Find(SigilKind kind)
    {
        for (int i = 0; i < Owned.Count; i++)
            if (Owned[i].def.kind == kind) return Owned[i];
        return null;
    }
    public bool Has(SigilKind kind)
    {
        return Find(kind) != null;
    }
    public bool Learn(SigilKind kind)
    {
        if (Has(kind) || Owned.Count >= maxSigils) return false;
        SigilInstance s = new SigilInstance(SigilLibrary.Get(kind));
        s.timer = 0.4f;
        s.lastDropPos = transform.position;
        Owned.Add(s);
        OnSigilsChanged?.Invoke();
        return true;
    }
    public bool Upgrade(SigilKind kind)
    {
        SigilInstance s = Find(kind);
        if (s == null || s.level >= SigilInstance.MaxLevel) return false;

        s.level++;
        OnSigilsChanged?.Invoke();
        return true;
    }

    public float DamageOf(SigilInstance s)
    {
        return s.def.damage * s.LevelMul * DamageMultiplier * (1f + ZoneSystem.AuraDamage);
    }

    public float CooldownOf(SigilInstance s)
    {
        return s.def.cooldown * Mathf.Pow(0.93f, s.level - 1) * CooldownMultiplier;
    }
    public float RadiusOf(SigilInstance s)
    {
        return s.def.radius * (1f + 0.06f * (s.level - 1));
    }
    public int CountOf(SigilInstance s)
    {
        if (s.def.kind == SigilKind.Nova) return s.def.count + 2 * (s.level - 1);
        return s.def.count + (s.level - 1) / 2;
    }
    void Update()
    {
        float dt = Time.deltaTime;
        float castDt = dt * (1f + ZoneSystem.AuraCast);
        for (int i = 0; i < Owned.Count; i++)
        {
            SigilInstance s = Owned[i];
            s.timer -= castDt;
            if (s.def.kind == SigilKind.Spirit)
            {
                if (s.timer < 0f) s.timer = 0f;
                continue;
            }

            if (s.timer > 0f) continue;
            bool cast;
            if (s.def.kind == SigilKind.Reprise)
                cast = lastCast != null && SigilCasts.Cast(this, lastCast);
            else
                cast = SigilCasts.Cast(this, s);
            if (cast)
            {
                if (s.def.kind == SigilKind.Reprise) SigilVfxSystem.Instance.Cast(transform.position, Color.white, 0.9f);
                s.timer = CooldownOf(s);
                if (s.def.kind != SigilKind.Reprise) lastCast = s;
            }
            else
            {
                s.timer = 0f;
            }
        }
    }

    private void HandleEnemyDied(Vector2 position)
    {
        for (int i = 0; i < Owned.Count; i++)
        {
            SigilInstance s = Owned[i];
            if (s.def.kind != SigilKind.Spirit) continue;
            SigilCasts.Spirits(this, s, position);
        }
    }
}