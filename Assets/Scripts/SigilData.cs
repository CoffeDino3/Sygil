using UnityEngine;

[CreateAssetMenu(fileName = "NewSigil", menuName = "Sygil/Sigil Data")]
public class SigilData : ScriptableObject
{
    public string displayName = "Bolt";
    public Projectile projectilePrefab;
    [Header("Stats")]
    public float damage = 1f;
    public float cooldown = 0.8f;
    public float projectileSpeed = 10f;
    public float targetRange = 8f;
    public float lifetime = 2f;
    public int pierce = 0;
}