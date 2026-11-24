using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class SygilUI : MonoBehaviour
{
    [Header("Optional: drop a TMP font asset here (e.g. Fredoka)")]
    [SerializeField] private TMP_FontAsset font;

    [Header("Level-up choices")]
    [Range(1, 5)]
    [SerializeField] int choiceCount = 3;
    [SerializeField] int refreshCharges = 2;
    [SerializeField] int skipCharges = 2;
    public int ChoiceCount
    {
        get => choiceCount;
        set => choiceCount = Mathf.Clamp(value, 1, MaxCards);
    }
    public void AddRefreshes(int amount) { refreshCharges += amount; RefreshButtons(); }
    public void AddSkips(int amount) { skipCharges += amount; RefreshButtons(); }

    static readonly Color Plum = new Color32(0x1B, 0x15, 0x30, 0xFF);
    private static readonly Color Dusk = new Color32(0x2E, 0x24, 0x50, 0xFF);

    const int MaxCards = 5;
    const float AreaHeight = 1080f;
    const float Spacing = 28f;
    private const float MaxCardHeight = 300f;


    PlayerXP xp;
    Health health;
    private SigilCaster caster;
    private PlayerController mover;
    Sprite cardSprite;
    Sprite smallSprite;
    Sprite circleSprite;
    private readonly Dictionary<string, Sprite> glyphs = new Dictionary<string, Sprite>();

    private Image xpFill;
    Image hpFill;
    private RectTransform levelPill;
    TMP_Text levelText;
    TMP_Text hpText;
    private TMP_Text timerText;
    private TMP_Text sigilListText;
    Image damageFlash;
    float xpGoal, hpGoal = 1f;
    private float xpShown, hpShown = 1f;
    float lastHp;
    float flash;
    float punch;
    float elapsed;
    int shownSecond = -1;
    class Card
    {
        public RectTransform root;
        public LayoutElement layout;
        public Image border, badge, glyph, pill;
        public TMP_Text kind, title, desc;
        public Button button;
        public Coroutine anim;
    }
    private CanvasGroup overlay;
    RectTransform titleRect;
    readonly Card[] cards = new Card[MaxCards];
    readonly List<UpgradeOption> pool = new List<UpgradeOption>();
    readonly List<UpgradeOption> shown = new List<UpgradeOption>();
    int pending;
    bool open;


    private readonly Dictionary<SigilKind, UpgradeOption> learnOptions = new Dictionary<SigilKind, UpgradeOption>();
    readonly Dictionary<SigilKind, UpgradeOption> upgradeOptions = new Dictionary<SigilKind, UpgradeOption>();


    Button refreshBtn, skipBtn;
    private Image refreshImg, skipImg;
    TMP_Text refreshLabel, skipLabel;
    private void Start()
    {
        xp = FindFirstObjectByType<PlayerXP>();
        if (xp == null)
        {
            Debug.LogError("SygilUI: couldn't find PlayerXP in the scene. Is the Player in the scene, with PlayerXP on it?");
            enabled = false;
            return;
        }

        health = xp.GetComponent<Health>();
        caster = xp.GetComponent<SigilCaster>();
        mover = xp.GetComponent<PlayerController>();

        BuildSprites();
        EnsureEventSystem();
        RectTransform canvas = BuildCanvas();
        BuildHud(canvas);
        BuildLevelUp(canvas);
        BuildPool();
        BuildSigilOptions();
        foreach (UpgradeOption o in pool) GlyphFor(o.title);

        xp.OnXpChanged += HandleXp;
        xp.OnLevelUp += HandleLevelUp;
        health.OnHealthChanged += HandleHealth;
        caster.OnSigilsChanged += RefreshSigilList;
        RefreshSigilList();
        lastHp = health.Current;
        hpGoal = hpShown = health.Current / health.Max;
        hpText.text = $"{Mathf.CeilToInt(health.Current)} / {Mathf.CeilToInt(health.Max)}";
        levelText.text = $"Lv {xp.Level}";
        RefreshButtons();
    }
    private void OnDestroy()
    {
        if (xp != null)
        {
            xp.OnXpChanged -= HandleXp;
            xp.OnLevelUp -= HandleLevelUp;
        }
        if (health != null) health.OnHealthChanged -= HandleHealth;
        if (caster != null) caster.OnSigilsChanged -= RefreshSigilList;
        if (open) Time.timeScale = 1f;
    }
    private void HandleXp(int current, int needed)
    {
        xpGoal = (float)current / needed;
    }

    void HandleLevelUp(int level)
    {
        levelText.text = $"Lv {level}";
        xpShown = 0f;
        punch = 1f;

        pending++;
        if (!open) ShowChoices();
    }

    void HandleHealth(float current, float max)
    {
        hpGoal = current / max;
        if (current < lastHp) flash = 0.35f;
        lastHp = current;
        hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        int sec = (int)elapsed;
        if (sec != shownSecond)
        {
            shownSecond = sec;
            timerText.text = $"{sec / 60}:{sec % 60:00}";
        }


        float dt = Time.unscaledDeltaTime;
        float k = 1f - Mathf.Exp(-12f * dt);

        xpShown = Mathf.Lerp(xpShown, xpGoal, k);
        hpShown = Mathf.Lerp(hpShown, hpGoal, k);
        SetBar(xpFill, xpShown);
        SetBar(hpFill, hpShown);
        flash = Mathf.MoveTowards(flash, 0f, dt * 1.6f);
        Color c = SygilPalette.Coral;
        damageFlash.color = new Color(c.r, c.g, c.b, flash);
        punch = Mathf.MoveTowards(punch, 0f, dt * 2.5f);
        levelPill.localScale = Vector3.one * (1f + punch * 0.35f);
        overlay.alpha = Mathf.MoveTowards(overlay.alpha, open ? 1f : 0f, dt * 6f);
        if (open)
        {
            float t = Time.unscaledTime;
            titleRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.2f) * 2.5f);
            titleRect.localScale = Vector3.one * (1f + Mathf.Sin(t * 3.1f) * 0.03f);
        }
    }

    static void SetBar(Image fill, float value)
    {
        value = Mathf.Clamp01(value);
        fill.enabled = value > 0.01f;
        fill.rectTransform.anchorMax = new Vector2(value, 1f);
    }

    void BuildPool()
    {
        pool.Add(new UpgradeOption("Sharper Bolts", "+20% damage", UpgradeKind.Stat,
            () => caster.DamageMultiplier += 0.2f));
        pool.Add(new UpgradeOption("Quick Fingers", "Cast 12% faster", UpgradeKind.Stat,
            () => caster.CooldownMultiplier *= 0.88f));
        pool.Add(new UpgradeOption("Light Feet", "+10% move speed", UpgradeKind.Stat,
            () => mover.SpeedMultiplier += 0.1f));
        pool.Add(new UpgradeOption("Long Stride", "+20% move speed", UpgradeKind.Stat,
            () => mover.SpeedMultiplier += 0.2f));
        pool.Add(new UpgradeOption("Heavy Hand", "+50% damage, but cast 15% slower", UpgradeKind.Stat,
            () => { caster.DamageMultiplier += 0.5f; caster.CooldownMultiplier *= 1.15f; }));
        pool.Add(new UpgradeOption("Frenzy", "Cast 25% faster, but -10% damage", UpgradeKind.Behavior,
            () => { caster.CooldownMultiplier *= 0.75f; caster.DamageMultiplier = Mathf.Max(0.2f, caster.DamageMultiplier - 0.1f); }));
        pool.Add(new UpgradeOption("Magnet Charm", "+1 pickup range", UpgradeKind.Trigger,
            () => xp.AddPickupRange(1f)));
        pool.Add(new UpgradeOption("Warm Tea", "Heal 3 HP", UpgradeKind.Element,
            () => health.Heal(3f)));
        pool.Add(new UpgradeOption("Second Wind", "Heal 6 HP", UpgradeKind.Element,
            () => health.Heal(6f)));
        pool.Add(new UpgradeOption("Piercing Thought", "Bolts pass through +1 enemy", UpgradeKind.Behavior,
            () => caster.BonusPierce++));
        pool.Add(new UpgradeOption("Ghost Bolt", "Pierce +2 enemies, but -10% damage", UpgradeKind.Behavior,
            () => { caster.BonusPierce += 2; caster.DamageMultiplier = Mathf.Max(0.2f, caster.DamageMultiplier - 0.1f); }));
        pool.Add(new UpgradeOption("Static Charge", "Bolts crackle with lightning, +15% damage", UpgradeKind.Element,
            () => { caster.BonusFlags |= ShotFlags.Crackle; caster.DamageMultiplier += 0.15f; }));
    }

    private void ShowChoices()
    {
        open = true;
        Time.timeScale = 0f;
        overlay.interactable = true;
        overlay.blocksRaycasts = true;

        RollChoices(false);
        RefreshButtons();
    }

    private List<UpgradeOption> Candidates()
    {
        List<UpgradeOption> list = new List<UpgradeOption>(pool);

        foreach (SigilKind kind in System.Enum.GetValues(typeof(SigilKind)))
        {
            SigilInstance owned = caster.Find(kind);
            if (owned == null)
            {
                if (caster.Owned.Count < caster.MaxSigils) list.Add(learnOptions[kind]);
            }
            else if (owned.level < SigilInstance.MaxLevel)
            {
                UpgradeOption up = upgradeOptions[kind];
                up.title = $"{owned.def.name} Lv {owned.level + 1}";
                up.description = "+25% damage, casts 7% faster";
                list.Add(up);
            }
        }
        return list;
    }
    private void BuildSigilOptions()
    {
        foreach (SigilKind kind in System.Enum.GetValues(typeof(SigilKind)))
        {
            SigilKind k = kind;
            SigilDef def = SigilLibrary.Get(kind);
            learnOptions[kind] = new UpgradeOption(def.name, def.description, UpgradeKind.Sigil,
                () => caster.Learn(k))
            { glyphKey = def.name };
            upgradeOptions[kind] = new UpgradeOption(def.name, "", UpgradeKind.Sigil,
                () => caster.Upgrade(k))
            { glyphKey = def.name };

            GlyphFor(def.name);
        }
    }


    private void RefreshSigilList()
    {
        if (sigilListText == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (SigilInstance s in caster.Owned)
            sb.AppendLine($"{s.def.name}  Lv {s.level}");
        sigilListText.text = sb.ToString();
    }


    private void RollChoices(bool avoidShown)
    {
        List<UpgradeOption> candidates = Candidates();
        int n = Mathf.Min(Mathf.Clamp(choiceCount, 1, MaxCards), candidates.Count);
        HashSet<UpgradeOption> previous = new HashSet<UpgradeOption>();
        if (avoidShown) foreach (UpgradeOption o in shown) previous.Add(o);

        List<UpgradeOption> bag = new List<UpgradeOption>();
        foreach (UpgradeOption o in candidates)
            if (!previous.Contains(o)) bag.Add(o);
        if (bag.Count < n)
        {
            bag.Clear();
            bag.AddRange(candidates);
        }

        shown.Clear();
        float h = Mathf.Min(MaxCardHeight, (AreaHeight - Spacing * (n - 1)) / n);

        for (int i = 0; i < cards.Length; i++)
        {
            Card c = cards[i];
            if (i >= n)
            {
                c.root.gameObject.SetActive(false);
                continue;
            }

            int r = Random.Range(0, bag.Count);
            UpgradeOption option = bag[r];
            bag.RemoveAt(r);
            shown.Add(option);


            c.root.gameObject.SetActive(true);
            c.layout.preferredHeight = h;
            LayoutCard(c, h);
            SetupCard(c, option, i * 0.08f);
        }
    }
    void OnPicked(UpgradeOption option)
    {
        if (!open) return;
        option.apply();
        CloseOrNext();
    }

    void OnRefresh()
    {
        if (!open || refreshCharges <= 0) return;
        refreshCharges--;
        RollChoices(true);
        RefreshButtons();
    }

    void OnSkip()
    {
        if (!open || skipCharges <= 0) return;
        skipCharges--;
        CloseOrNext();
    }

    void CloseOrNext()
    {
        pending--;
        if (pending > 0)
        {
            RollChoices(false);
            RefreshButtons();
        }
        else
        {
            open = false;
            shown.Clear();
            overlay.interactable = false;
            overlay.blocksRaycasts = false;
            Time.timeScale = 1f;
        }
    }

    void SetupCard(Card c, UpgradeOption o, float delay)
    {
        Color accent = SygilPalette.ForKind(o.kind);
        c.border.color = accent;
        c.badge.color = accent;
        c.pill.color = accent;
        c.glyph.sprite = GlyphFor(string.IsNullOrEmpty(o.glyphKey) ? o.title : o.glyphKey);
        c.kind.text = o.kind.ToString().ToUpper();
        c.title.text = o.title;
        c.desc.text = o.description;


        c.button.onClick.RemoveAllListeners();
        c.button.onClick.AddListener(() => OnPicked(o));


        c.root.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-2.5f, 2.5f));
        if (c.anim != null) StopCoroutine(c.anim);
        c.anim = StartCoroutine(PopIn(c.root, delay));
    }

    void RefreshButtons()
    {
        if (refreshBtn == null) return;
        SetActionState(refreshBtn, refreshImg, refreshLabel, refreshCharges > 0,
                       $"Refresh ({refreshCharges})", SygilPalette.Cream, Plum);
        SetActionState(skipBtn, skipImg, skipLabel, skipCharges > 0,
                       $"Skip ({skipCharges})", Dusk, SygilPalette.Cream);
    }

    private static void SetActionState(Button b, Image img, TMP_Text label, bool enabled,
                                       string text, Color bg, Color fg)
    {
        float a = enabled ? 1f : 0.35f;
        bg.a = a;
        fg.a = a;

        b.interactable = enabled;
        img.color = bg;
        label.color = fg;
        label.text = text;
    }

    private IEnumerator PopIn(RectTransform rt, float delay)
    {
        rt.localScale = Vector3.zero;
        float t = -delay;
        const float duration = 0.35f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            rt.localScale = Vector3.one * EaseOutBack(k);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }
    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private void EnsureEventSystem()
    {
        EventSystem es = FindFirstObjectByType<EventSystem>();
        if (es == null)
            es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();


        if (es.GetComponent<InputSystemUIInputModule>() == null)
        {
            StandaloneInputModule old = es.GetComponent<StandaloneInputModule>();
            if (old != null) Destroy(old);


            InputSystemUIInputModule module = es.gameObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }
    }

    private RectTransform BuildCanvas()
    {
        GameObject go = new GameObject("SygilCanvas");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return (RectTransform)go.transform;
    }


    void BuildHud(RectTransform canvas)
    {
        damageFlash = NewImage("DamageFlash", canvas, null, new Color(1f, 0.54f, 0.48f, 0f), false);
        Stretch(damageFlash.rectTransform);

        RectTransform hud = NewRect("HUD", canvas);
        Rect safe = Screen.safeArea;
        hud.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        hud.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        hud.offsetMin = Vector2.zero;
        hud.offsetMax = Vector2.zero;
        Image xpBg = NewBar("XpBar", hud, SygilPalette.Butter, out xpFill);
        RectTransform xpRt = xpBg.rectTransform;
        xpRt.anchorMin = new Vector2(0f, 1f);
        xpRt.anchorMax = new Vector2(1f, 1f);
        xpRt.pivot = new Vector2(0.5f, 1f);
        xpRt.anchoredPosition = new Vector2(0f, -50f);
        xpRt.sizeDelta = new Vector2(-80f, 30f);
        Image hpBg = NewBar("HpBar", hud, SygilPalette.Coral, out hpFill);
        Place(hpBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -96f), new Vector2(440f, 30f));
        hpText = NewText("HpText", hud, "10 / 10", 30, SygilPalette.Cream, TextAlignmentOptions.MidlineLeft, false);
        Place(hpText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(500f, -111f), new Vector2(220f, 40f));

        Image pill = NewImage("LevelPill", hud, smallSprite, SygilPalette.Butter, true);
        levelPill = pill.rectTransform;
        Place(levelPill, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(120f, -169f), new Vector2(160f, 46f));
        levelText = NewText("LevelText", levelPill, "Lv 1", 30, Plum, TextAlignmentOptions.Center, true);
        Stretch(levelText.rectTransform);
        Color dimCream = SygilPalette.Cream;
        dimCream.a = 0.8f;
        sigilListText = NewText("SigilList", hud, "", 28, dimCream, TextAlignmentOptions.TopLeft, false);
        Place(sigilListText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -215f), new Vector2(520f, 420f));
        timerText = NewText("Timer", hud, "0:00", 36, SygilPalette.Cream, TextAlignmentOptions.MidlineRight, true);
        Place(timerText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-40f, -111f), new Vector2(220f, 40f));
    }

    void BuildLevelUp(RectTransform canvas)
    {
        Image bg = NewImage("LevelUpOverlay", canvas, null, new Color(Plum.r, Plum.g, Plum.b, 0.88f), false);
        bg.raycastTarget = true;
        Stretch(bg.rectTransform);
        overlay = bg.gameObject.AddComponent<CanvasGroup>();
        overlay.alpha = 0f;
        overlay.interactable = false;
        overlay.blocksRaycasts = false;

        Vector2 mid = new Vector2(0.5f, 0.5f);
        TMP_Text title = NewText("Title", bg.transform, "LEVEL UP!", 120, SygilPalette.Butter, TextAlignmentOptions.Center, true);
        titleRect = title.rectTransform;
        Place(titleRect, mid, mid, new Vector2(0f, 670f), new Vector2(960f, 160f));
        Color dim = SygilPalette.Cream;
        dim.a = 0.75f;
        TMP_Text sub = NewText("Subtitle", bg.transform, "pick a boon", 44, dim, TextAlignmentOptions.Center, false);
        Place(sub.rectTransform, mid, mid, new Vector2(0f, 570f), new Vector2(900f, 60f));
        RectTransform cardsRoot = NewRect("Cards", bg.transform);
        Place(cardsRoot, mid, mid, new Vector2(0f, -20f), new Vector2(920f, AreaHeight));
        VerticalLayoutGroup layout = cardsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = Spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        for (int i = 0; i < cards.Length; i++)
            cards[i] = BuildCard(cardsRoot);
        BuildActionButton(bg.transform, "Refresh", new Vector2(-210f, -700f),
                          out refreshBtn, out refreshImg, out refreshLabel);
        BuildActionButton(bg.transform, "Skip", new Vector2(210f, -700f),
                          out skipBtn, out skipImg, out skipLabel);
        refreshBtn.onClick.AddListener(OnRefresh);
        skipBtn.onClick.AddListener(OnSkip);
    }

    private void BuildActionButton(Transform parent, string name, Vector2 pos,
                                   out Button btn, out Image img, out TMP_Text label)
    {
        img = NewImage(name + "Button", parent, cardSprite, Dusk, true);
        img.raycastTarget = true;
        Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(380f, 96f));

        btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.None;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        label = NewText("Label", img.transform, name, 36, SygilPalette.Cream, TextAlignmentOptions.Center, true);
        Stretch(label.rectTransform);
    }
    Card BuildCard(RectTransform parent)
    {
        Card c = new Card();

        c.border = NewImage("Card", parent, cardSprite, Color.white, true);
        c.border.raycastTarget = true;
        c.root = c.border.rectTransform;
        c.layout = c.border.gameObject.AddComponent<LayoutElement>();
        c.layout.preferredHeight = MaxCardHeight;

        c.button = c.border.gameObject.AddComponent<Button>();
        c.button.targetGraphic = c.border;
        c.button.transition = Selectable.Transition.None;
        c.button.navigation = new Navigation { mode = Navigation.Mode.None };
        Image inner = NewImage("Inner", c.root, cardSprite, Dusk, true);
        Stretch(inner.rectTransform, 6f, 6f, 6f, 6f);
        c.badge = NewImage("Badge", inner.transform, circleSprite, Color.white, false);
        c.glyph = NewImage("Glyph", c.badge.transform, null, Plum, false);
        c.glyph.preserveAspect = true;
        Stretch(c.glyph.rectTransform, 18f, 18f, 18f, 18f);
        c.pill = NewImage("KindPill", inner.transform, smallSprite, Color.white, true);
        c.kind = NewText("Kind", c.pill.transform, "STAT", 24, Plum, TextAlignmentOptions.Center, true);
        Stretch(c.kind.rectTransform);
        c.title = NewText("Title", inner.transform, "Title", 54, SygilPalette.Cream, TextAlignmentOptions.MidlineLeft, true);
        c.title.enableAutoSizing = true;
        c.title.fontSizeMin = 28f;
        c.title.fontSizeMax = 54f;

        Color dim = SygilPalette.Cream;
        dim.a = 0.8f;
        c.desc = NewText("Description", inner.transform, "Description", 34, dim, TextAlignmentOptions.TopLeft, false);

        LayoutCard(c, MaxCardHeight);
        return c;
    }
    private void LayoutCard(Card c, float h)
    {
        bool compact = h < 240f;
        float badge = Mathf.Min(130f, h - 50f);
        Vector2 topLeft = new Vector2(0f, 1f);
        Place(c.badge.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
              new Vector2(28f, 0f), new Vector2(badge, badge));
        float left = 28f + badge + 26f;
        if (!compact)
        {
            Place(c.pill.rectTransform, topLeft, topLeft, new Vector2(left, -22f), new Vector2(180f, 38f));
            c.kind.fontSize = 24f;
            c.title.fontSizeMax = 54f;
            Box(c.title.rectTransform, left, 68f, 24f, 72f);
            Box(c.desc.rectTransform, left, 146f, 24f, Mathf.Max(40f, h - 12f - 146f - 10f));
        }
        else
        {
            Vector2 topRight = new Vector2(1f, 1f);
            Place(c.pill.rectTransform, topRight, topRight, new Vector2(-22f, -16f), new Vector2(150f, 32f));
            c.kind.fontSize = 20f;
            c.title.fontSizeMax = 44f;
            Box(c.title.rectTransform, left, 14f, 190f, 54f);
            Box(c.desc.rectTransform, left, 70f, 24f, Mathf.Max(36f, h - 12f - 70f - 8f));
        }
    }

    private RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    Image NewImage(string name, Transform parent, Sprite sprite, Color color, bool sliced)
    {
        RectTransform rt = NewRect(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        img.raycastTarget = false;
        return img;
    }
    private TMP_Text NewText(string name, Transform parent, string text, float size, Color color,
                             TextAlignmentOptions align, bool bold)
    {
        RectTransform rt = NewRect(name, parent);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.raycastTarget = false;
        return t;
    }

    private Image NewBar(string name, Transform parent, Color fillColor, out Image fill)
    {
        Image bg = NewImage(name, parent, smallSprite, Dusk, true);

        RectTransform track = NewRect("Track", bg.transform);
        Stretch(track, 4f, 4f, 4f, 4f);

        fill = NewImage("Fill", track, smallSprite, fillColor, true);
        RectTransform f = fill.rectTransform;
        f.anchorMin = Vector2.zero;
        f.anchorMax = new Vector2(0f, 1f);
        f.offsetMin = Vector2.zero;
        f.offsetMax = Vector2.zero;
        return bg;
    }

    static void Stretch(RectTransform rt, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }


    static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Box(RectTransform rt, float left, float top, float right, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(left, -(top + height));
        rt.offsetMax = new Vector2(-right, -top);
    }
    void BuildSprites()
    {
        cardSprite = MakeRounded(64, 22f, 24f);
        smallSprite = MakeRounded(32, 12f, 12f);
        circleSprite = MakeRounded(128, 64f, 0f);
    }
    private static Sprite MakeRounded(int size, float radius, float border)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float half = size * 0.5f;
        Color32[] px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - half) - (half - radius);
                float dy = Mathf.Abs(y + 0.5f - half) - (half - radius);
                float outside = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude;
                float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
                float d = outside + inside - radius;
                byte a = (byte)(Mathf.Clamp01(0.5f - d) * 255f);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                             SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }

    private Sprite GlyphFor(string key)
    {
        if (!glyphs.TryGetValue(key, out Sprite sprite))
        {
            sprite = MakeGlyph(StableHash(key));
            glyphs[key] = sprite;
        }
        return sprite;
    }

    static int StableHash(string s)
    {
        unchecked
        {
            int h = (int)2166136261;
            foreach (char ch in s)
            {
                h ^= ch;
                h *= 16777619;
            }
            return h;
        }
    }

    static Sprite MakeGlyph(int seed)
    {
        const int size = 96;
        System.Random rng = new System.Random(seed);

        Vector2[] lattice = new Vector2[9];
        for (int gy = 0; gy < 3; gy++)
            for (int gx = 0; gx < 3; gx++)
                lattice[gy * 3 + gx] = new Vector2(0.2f + gx * 0.3f, 0.2f + gy * 0.3f);

        int want = 3 + rng.Next(3);
        List<Vector4> segs = new List<Vector4>();
        List<Vector2> ends = new List<Vector2>();

        for (int guard = 0; segs.Count < want && guard < 60; guard++)
        {
            bool fresh = ends.Count == 0 || rng.NextDouble() < 0.25;
            Vector2 a = fresh ? lattice[rng.Next(9)] : ends[rng.Next(ends.Count)];
            Vector2 b = lattice[rng.Next(9)];

            if ((a - b).sqrMagnitude < 0.05f) continue;
            if (HasSegment(segs, a, b)) continue;

            segs.Add(new Vector4(a.x, a.y, b.x, b.y));
            ends.Add(a);
            ends.Add(b);
        }
        const float halfThick = 0.036f;
        Color32[] px = new Color32[size * size];


        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                float d = float.MaxValue;
                for (int s = 0; s < segs.Count; s++)
                    d = Mathf.Min(d, DistToSegment(p, segs[s]));
                float signed = d - halfThick;
                byte alpha = (byte)(Mathf.Clamp01(0.5f - signed * size) * 255f);
                px[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(px);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static bool HasSegment(List<Vector4> segs, Vector2 a, Vector2 b)
    {
        foreach (Vector4 s in segs)
        {
            Vector2 p = new Vector2(s.x, s.y);
            Vector2 q = new Vector2(s.z, s.w);
            if ((p == a && q == b) || (p == b && q == a)) return true;
        }
        return false;
    }

    private static float DistToSegment(Vector2 p, Vector4 seg)
    {
        Vector2 a = new Vector2(seg.x, seg.y);
        Vector2 b = new Vector2(seg.z, seg.w);
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
        return Vector2.Distance(p, a + ab * t);
    }
}