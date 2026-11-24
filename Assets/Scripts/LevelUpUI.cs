using System.Collections.Generic;
using UnityEngine;

public class LevelUpUI : MonoBehaviour
{
    [SerializeField] CanvasGroup root;
    [SerializeField] private UpgradeCardUI[] cards;
    [SerializeField] PlayerXP xp;
    [SerializeField] private SigilCaster caster;
    [SerializeField] PlayerController mover;
    [SerializeField] Health health;
    private readonly List<UpgradeOption> pool = new List<UpgradeOption>();
    int pending;
    bool open;

    void Awake()
    {
        BuildPool();
        SetVisible(false);
    }
    private void OnEnable() { xp.OnLevelUp += HandleLevelUp; }
    private void OnDisable() { xp.OnLevelUp -= HandleLevelUp; }

    void BuildPool()
    {
        pool.Add(new UpgradeOption("Sharper Bolts", "+20% damage", UpgradeKind.Stat,
            () => caster.DamageMultiplier += 0.2f));
        pool.Add(new UpgradeOption("Quick Fingers", "Cast 12% faster", UpgradeKind.Stat,
            () => caster.CooldownMultiplier *= 0.88f));
        pool.Add(new UpgradeOption("Light Feet", "+10% move speed", UpgradeKind.Stat,
            () => mover.SpeedMultiplier += 0.1f));
        pool.Add(new UpgradeOption("Magnet Charm", "+1 pickup range", UpgradeKind.Trigger,
            () => xp.AddPickupRange(1f)));
        pool.Add(new UpgradeOption("Warm Tea", "Heal 3 HP", UpgradeKind.Element,
            () => health.Heal(3f)));
        pool.Add(new UpgradeOption("Piercing Thought", "Bolts pass through +1 enemy", UpgradeKind.Behavior,
            () => caster.BonusPierce++));
    }

    void HandleLevelUp(int level)
    {
        pending++;
        if (!open) ShowChoices();
    }
    void ShowChoices()
    {
        open = true;
        Time.timeScale = 0f;
        SetVisible(true);
        List<UpgradeOption> bag = new List<UpgradeOption>(pool);
        for (int i = 0; i < cards.Length && bag.Count > 0; i++)
        {
            int r = Random.Range(0, bag.Count);
            cards[i].Setup(bag[r], OnPicked, i * 0.08f);
            bag.RemoveAt(r);
        }
    }
    void OnPicked(UpgradeOption option)
    {
        option.apply();
        pending--;

        if (pending > 0)
        {
            ShowChoices();
        }
        else
        {
            open = false;
            SetVisible(false);
            Time.timeScale = 1f;
        }
    }
    private void SetVisible(bool visible)
    {
        root.alpha = visible ? 1f : 0f;
        root.interactable = visible;
        root.blocksRaycasts = visible;
    }
}