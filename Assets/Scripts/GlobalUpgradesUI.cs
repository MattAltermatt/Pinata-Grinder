using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the "Upgrades" trigger button (below Buy Stopper) and the
/// full-screen overlay panel with 6 global upgrade rows.
/// Game continues running underneath the semi-transparent overlay.
/// </summary>
public class GlobalUpgradesUI : MonoBehaviour
{
    private const int RowCount = 6;
    private static readonly Color DarkBg = new(0.15f, 0.15f, 0.2f, 0.85f);
    private static readonly Color AffordBg = new(0.2f, 0.2f, 0.25f, 0.9f);
    private static readonly Color CantAffordBg = new(0.3f, 0.1f, 0.1f, 0.6f);

    private GameObject _overlay;

    private readonly Button[] _buyBtns = new Button[RowCount];
    private readonly Image[] _buyBgs = new Image[RowCount];
    private readonly Text[] _costLabels = new Text[RowCount];
    private readonly Text[] _descLabels = new Text[RowCount];
    private readonly GameObject[] _soldOutLabels = new GameObject[RowCount];

    private Font _font;

    void Start()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildTriggerButton();
        BuildOverlay();
        _overlay.SetActive(false);

        Economy.Instance.OnMoneyChanged += OnMoneyChanged;
        GlobalUpgrades.Instance.OnUpgradeChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (Economy.Instance != null)
            Economy.Instance.OnMoneyChanged -= OnMoneyChanged;
        if (GlobalUpgrades.Instance != null)
            GlobalUpgrades.Instance.OnUpgradeChanged -= Refresh;
    }

    void OnMoneyChanged(int _) => Refresh();

    // ── Trigger button (always visible, below Buy Stopper) ──

    void BuildTriggerButton()
    {
        var canvas = EconomyUI.Instance.Canvas;

        var go = new GameObject("UpgradesButton");
        go.transform.SetParent(canvas.transform, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -170f);
        rect.sizeDelta = new Vector2(140f, 140f);

        var bg = go.AddComponent<Image>();
        bg.color = DarkBg;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(ToggleOverlay);

        // Icon
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform, false);
        var iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.6f);
        iconRect.anchorMax = new Vector2(0.5f, 0.6f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(60f, 60f);

        var iconImg = iconGo.AddComponent<Image>();
        iconImg.sprite = GameField.WallExpandSprite();
        iconImg.color = new Color(0.4f, 0.8f, 1f);
        iconImg.raycastTarget = false;

        // Label
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0.35f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelGo.AddComponent<Text>();
        label.text = "Upgrades";
        label.font = _font;
        label.fontSize = 24;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    // ── Full-screen overlay ──

    void BuildOverlay()
    {
        var canvas = EconomyUI.Instance.Canvas;

        // Full-screen backdrop
        _overlay = new GameObject("GlobalUpgradesOverlay");
        _overlay.transform.SetParent(canvas.transform, false);

        var backdropRect = _overlay.AddComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        var backdropImg = _overlay.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.5f);
        backdropImg.raycastTarget = true;

        // Center panel
        var panel = new GameObject("Panel");
        panel.transform.SetParent(_overlay.transform, false);

        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(700f, 1050f);

        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // Title
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -10f);
        titleRect.sizeDelta = new Vector2(0f, 70f);

        var titleText = titleGo.AddComponent<Text>();
        titleText.text = "GLOBAL UPGRADES";
        titleText.font = _font;
        titleText.fontSize = 40;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.raycastTarget = false;

        // Close button
        var closeGo = new GameObject("CloseButton");
        closeGo.transform.SetParent(panel.transform, false);
        var closeRect = closeGo.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-5f, -5f);
        closeRect.sizeDelta = new Vector2(60f, 60f);

        var closeBg = closeGo.AddComponent<Image>();
        closeBg.color = new Color(0.5f, 0.15f, 0.1f, 0.9f);

        var closeBtn = closeGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeBg;
        closeBtn.onClick.AddListener(() => _overlay.SetActive(false));

        var xLabelGo = new GameObject("XLabel");
        xLabelGo.transform.SetParent(closeGo.transform, false);
        var xRect = xLabelGo.AddComponent<RectTransform>();
        xRect.anchorMin = Vector2.zero;
        xRect.anchorMax = Vector2.one;
        xRect.offsetMin = Vector2.zero;
        xRect.offsetMax = Vector2.zero;

        var xLabel = xLabelGo.AddComponent<Text>();
        xLabel.text = "X";
        xLabel.font = _font;
        xLabel.fontSize = 36;
        xLabel.fontStyle = FontStyle.Bold;
        xLabel.alignment = TextAnchor.MiddleCenter;
        xLabel.color = Color.white;
        xLabel.raycastTarget = false;

        // Upgrade rows
        float rowHeight = 140f;
        float rowGap = 10f;
        float topOffset = -90f;

        BuildUpgradeRow(panel.transform, 0, "Wall Size", GameField.WallExpandSprite(),
            new Color(0.3f, 0.7f, 1f), topOffset, rowHeight);
        BuildUpgradeRow(panel.transform, 1, "Pinata Size", GameField.GridSprite(),
            new Color(1f, 0.6f, 0.2f), topOffset - (rowHeight + rowGap), rowHeight);
        BuildUpgradeRow(panel.transform, 2, "Spawn Rate", GameField.ClockSprite(),
            new Color(0.4f, 1f, 0.4f), topOffset - (rowHeight + rowGap) * 2f, rowHeight);
        BuildUpgradeRow(panel.transform, 3, "Oscillation", GameField.WaveSprite(),
            new Color(1f, 0.4f, 0.8f), topOffset - (rowHeight + rowGap) * 3f, rowHeight);
        BuildUpgradeRow(panel.transform, 4, "Pinata HP", GameField.HeartSprite(),
            new Color(1f, 0.3f, 0.3f), topOffset - (rowHeight + rowGap) * 4f, rowHeight);
        BuildUpgradeRow(panel.transform, 5, "Death Line", GameField.BoltSprite(),
            new Color(1f, 1f, 0.3f), topOffset - (rowHeight + rowGap) * 5f, rowHeight);
    }

    void BuildUpgradeRow(Transform parent, int index, string upgradeName, Sprite icon,
                          Color iconColor, float yOffset, float height)
    {
        var row = new GameObject(upgradeName + "Row");
        row.transform.SetParent(parent, false);

        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, yOffset);
        rowRect.sizeDelta = new Vector2(-30f, height);

        var rowBg = row.AddComponent<Image>();
        rowBg.color = DarkBg;

        // Icon
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(row.transform, false);
        var iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(15f, 0f);
        iconRect.sizeDelta = new Vector2(80f, 80f);

        var iconImg = iconGo.AddComponent<Image>();
        iconImg.sprite = icon;
        iconImg.color = iconColor;
        iconImg.raycastTarget = false;

        // Sold out overlay on icon
        var soldOutGo = new GameObject("SoldOut");
        soldOutGo.transform.SetParent(iconGo.transform, false);
        var soldOutRect = soldOutGo.AddComponent<RectTransform>();
        soldOutRect.anchorMin = Vector2.zero;
        soldOutRect.anchorMax = Vector2.one;
        soldOutRect.offsetMin = Vector2.zero;
        soldOutRect.offsetMax = Vector2.zero;

        var soldOutBg = soldOutGo.AddComponent<Image>();
        soldOutBg.color = new Color(0f, 0f, 0f, 0.7f);

        var soldOutTextGo = new GameObject("Text");
        soldOutTextGo.transform.SetParent(soldOutGo.transform, false);
        var soldOutTextRect = soldOutTextGo.AddComponent<RectTransform>();
        soldOutTextRect.anchorMin = Vector2.zero;
        soldOutTextRect.anchorMax = Vector2.one;
        soldOutTextRect.offsetMin = Vector2.zero;
        soldOutTextRect.offsetMax = Vector2.zero;

        var soldOutText = soldOutTextGo.AddComponent<Text>();
        soldOutText.text = "SOLD\nOUT";
        soldOutText.font = _font;
        soldOutText.fontSize = 20;
        soldOutText.fontStyle = FontStyle.Bold;
        soldOutText.alignment = TextAnchor.MiddleCenter;
        soldOutText.color = new Color(1f, 0.3f, 0.2f);
        soldOutText.raycastTarget = false;

        soldOutGo.SetActive(false);
        _soldOutLabels[index] = soldOutGo;

        // Name
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(row.transform, false);
        var nameRect = nameGo.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.5f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.offsetMin = new Vector2(110f, 0f);
        nameRect.offsetMax = new Vector2(-170f, -10f);

        var nameText = nameGo.AddComponent<Text>();
        nameText.text = upgradeName;
        nameText.font = _font;
        nameText.fontSize = 28;
        nameText.fontStyle = FontStyle.Bold;
        nameText.alignment = TextAnchor.MiddleLeft;
        nameText.color = Color.white;
        nameText.raycastTarget = false;

        // Description
        var descGo = new GameObject("Description");
        descGo.transform.SetParent(row.transform, false);
        var descRect = descGo.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0f, 0f);
        descRect.anchorMax = new Vector2(1f, 0.5f);
        descRect.offsetMin = new Vector2(110f, 10f);
        descRect.offsetMax = new Vector2(-170f, 0f);

        var descText = descGo.AddComponent<Text>();
        descText.font = _font;
        descText.fontSize = 22;
        descText.alignment = TextAnchor.MiddleLeft;
        descText.color = new Color(0.6f, 0.6f, 0.7f);
        descText.raycastTarget = false;
        _descLabels[index] = descText;

        // Buy button
        var buyGo = new GameObject("BuyButton");
        buyGo.transform.SetParent(row.transform, false);
        var buyRect = buyGo.AddComponent<RectTransform>();
        buyRect.anchorMin = new Vector2(1f, 0.5f);
        buyRect.anchorMax = new Vector2(1f, 0.5f);
        buyRect.pivot = new Vector2(1f, 0.5f);
        buyRect.anchoredPosition = new Vector2(-15f, 0f);
        buyRect.sizeDelta = new Vector2(140f, 70f);

        var buyBg = buyGo.AddComponent<Image>();
        buyBg.color = AffordBg;
        _buyBgs[index] = buyBg;

        var buyBtn = buyGo.AddComponent<Button>();
        buyBtn.targetGraphic = buyBg;
        _buyBtns[index] = buyBtn;

        int idx = index;
        buyBtn.onClick.AddListener(() => OnBuyClicked(idx));

        // Cost label
        var costGo = new GameObject("CostLabel");
        costGo.transform.SetParent(buyGo.transform, false);
        var costRect = costGo.AddComponent<RectTransform>();
        costRect.anchorMin = Vector2.zero;
        costRect.anchorMax = Vector2.one;
        costRect.offsetMin = Vector2.zero;
        costRect.offsetMax = Vector2.zero;

        var costText = costGo.AddComponent<Text>();
        costText.font = _font;
        costText.fontSize = 28;
        costText.fontStyle = FontStyle.Bold;
        costText.alignment = TextAnchor.MiddleCenter;
        costText.color = Color.white;
        costText.raycastTarget = false;
        _costLabels[index] = costText;
    }

    // ── Interactions ──

    void ToggleOverlay()
    {
        _overlay.SetActive(!_overlay.activeSelf);
        if (_overlay.activeSelf) Refresh();
    }

    void OnBuyClicked(int index)
    {
        var gm = GlobalUpgrades.Instance;
        switch (index)
        {
            case 0: gm.TryBuyWall(); break;
            case 1: gm.TryBuyPinata(); break;
            case 2: gm.TryBuySpawner(); break;
            case 3: gm.TryBuyOscillation(); break;
            case 4: gm.TryBuyHealth(); break;
            case 5: gm.TryBuyDeathLineDamage(); break;
        }
    }

    // ── Refresh ──

    void Refresh()
    {
        if (GlobalUpgrades.Instance == null) return;
        var gm = GlobalUpgrades.Instance;
        int money = Economy.Instance != null ? Economy.Instance.Money : 0;

        RefreshRow(0, gm.WallCost, money, gm.IsWallMaxed, WallDesc());
        RefreshRow(1, gm.PinataCost, money, gm.IsPinataMaxed, PinataDesc());
        RefreshRow(2, gm.SpawnerCost, money, gm.IsSpawnerMaxed, SpawnerDesc());
        RefreshRow(3, gm.OscillationCost, money, gm.IsOscillationMaxed, OscillationDesc());
        RefreshRow(4, gm.HealthCost, money, false, HealthDesc());
        RefreshRow(5, gm.DeathLineDamageCost, money, false, DeathLineDesc());
    }

    void RefreshRow(int index, int cost, int money, bool maxed, string desc)
    {
        _descLabels[index].text = desc;
        _soldOutLabels[index].SetActive(maxed);

        if (maxed)
        {
            _buyBtns[index].gameObject.SetActive(false);
        }
        else
        {
            _buyBtns[index].gameObject.SetActive(true);
            _costLabels[index].text = "$" + cost;

            bool canAfford = money >= cost;
            _buyBgs[index].color = canAfford ? AffordBg : CantAffordBg;
            _buyBtns[index].interactable = canAfford;
        }
    }

    // ── Description strings ──

    string WallDesc()
    {
        var gm = GlobalUpgrades.Instance;
        float w = gm.CalculateFieldWidth(gm.WallLevel);
        return gm.IsWallMaxed ? "Width: " + w.ToString("F1") + " (MAX)" : "Width: " + w.ToString("F1");
    }

    string PinataDesc()
    {
        int count = GlobalUpgrades.GetSquareCount(GlobalUpgrades.Instance.PinataLevel);
        return "Squares: " + count;
    }

    string SpawnerDesc()
    {
        var gm = GlobalUpgrades.Instance;
        float interval = gm.CalculateSpawnInterval(gm.SpawnerLevel);
        return "Rate: " + interval.ToString("F1") + "s  Lv " + gm.SpawnerLevel + "/20";
    }

    string OscillationDesc()
    {
        var gm = GlobalUpgrades.Instance;
        float period = gm.CalculateOscillationPeriod(gm.OscillationLevel);
        return "Period: " + period.ToString("F1") + "s  Lv " + gm.OscillationLevel + "/20";
    }

    string HealthDesc()
    {
        var gm = GlobalUpgrades.Instance;
        float hp = gm.CalculateHealth(gm.HealthLevel);
        return "HP: " + hp.ToString("F1") + "  ($" + Mathf.Max(1, Mathf.RoundToInt(hp * 2f)) + "/kill)";
    }

    string DeathLineDesc()
    {
        var gm = GlobalUpgrades.Instance;
        float dmg = gm.CalculateDeathLineDamage(gm.DeathLineDamageLevel);
        return "Damage: " + dmg.ToString("F1") + "  ($1/kill)";
    }
}
