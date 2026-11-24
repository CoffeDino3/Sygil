using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeCardUI : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text descText;
    [SerializeField] TMP_Text kindText;
    [SerializeField] private Image kindPill;
    [SerializeField] Outline outline;
    public void Setup(UpgradeOption option, Action<UpgradeOption> onPicked, float delay)
    {
        Color accent = SygilPalette.ForKind(option.kind);
        titleText.text = option.title;
        descText.text = option.description;
        kindText.text = option.kind.ToString().ToUpper();
        kindPill.color = accent;
        if (outline != null) outline.effectColor = accent;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onPicked(option));
        transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-2.5f, 2.5f));
        StopAllCoroutines();
        StartCoroutine(PopIn(delay));
    }
    IEnumerator PopIn(float delay)
    {
        transform.localScale = Vector3.zero;
        float t = -delay;
        const float duration = 0.35f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            transform.localScale = Vector3.one * EaseOutBack(k);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}