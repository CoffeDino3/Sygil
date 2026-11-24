using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class HudController : MonoBehaviour
{
    [SerializeField] PlayerXP xp;
    [SerializeField] Health playerHealth;
    [SerializeField] private Slider xpBar;
    [SerializeField] private Slider hpBar;
    [SerializeField] TMP_Text levelText;
    private float xpGoal;
    private float hpGoal = 1f;
    void OnEnable()
    {
        xp.OnXpChanged += HandleXp;
        xp.OnLevelUp += HandleLevel;
        playerHealth.OnHealthChanged += HandleHealth;
    }
    void OnDisable()
    {
        xp.OnXpChanged -= HandleXp;
        xp.OnLevelUp -= HandleLevel;
        playerHealth.OnHealthChanged -= HandleHealth;
    }
    void Start()
    {
        levelText.text = $"Lv {xp.Level}";
        xpBar.value = 0f;
        hpBar.value = 1f;
    }
    void HandleXp(int current, int needed)
    {
        xpGoal = (float)current / needed;
    }
    void HandleLevel(int level)
    {
        levelText.text = $"Lv {level}";
    }
    void HandleHealth(float cur, float max)
    {
        hpGoal = cur / max;
    }
    void Update()
    {
        float k = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
        xpBar.value = Mathf.Lerp(xpBar.value, xpGoal, k);
        hpBar.value = Mathf.Lerp(hpBar.value, hpGoal, k);
    }
}