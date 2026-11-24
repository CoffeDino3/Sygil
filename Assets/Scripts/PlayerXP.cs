using System;
using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    public static PlayerXP Instance { get; private set; }

    [SerializeField] float pickupRange = 1.3f;
    public int Level { get; private set; } = 1;
    public int CurrentXp { get; private set; }
    public int XpToNext => 5 + (Level - 1) * 4;
    public float PickupRange => pickupRange;
    public event Action<int, int> OnXpChanged;
    public event Action<int> OnLevelUp;
    void Awake()
    {
        Instance = this;
    }

    public void AddXp(int amount)
    {
        CurrentXp += amount;
        while (CurrentXp >= XpToNext)
        {
            CurrentXp -= XpToNext;
            Level++;
            OnLevelUp?.Invoke(Level);
        }
        OnXpChanged?.Invoke(CurrentXp, XpToNext);
    }
    public void AddPickupRange(float amount)
    {
        pickupRange += amount;
    }
}