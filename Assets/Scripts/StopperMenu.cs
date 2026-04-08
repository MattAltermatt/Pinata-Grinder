using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Popup menu that appears when a stopper is clicked.
/// No weapon: shows buy options for saw and laser (small panel near stopper).
/// Has weapon: shows full-screen upgrade overlay matching the GlobalUpgrades style,
/// with weapon name at top, upgrade rows, sell button at bottom, and close button.
/// </summary>
public class StopperMenu : MonoBehaviour
{
    public static StopperMenu Instance { get; private set; }

    private static readonly Color DarkBg = new(0.15f, 0.15f, 0.2f, 0.85f);
    private static readonly Color AffordBg = new(0.2f, 0.2f, 0.25f, 0.9f);
    private static readonly Color CantAffordBg = new(0.3f, 0.1f, 0.1f, 0.6f);
    private static readonly Color MaxedBg = new(0.35f, 0.3f, 0.1f, 0.9f);

    // Buy panel (small, near stopper)
    private GameObject _buyPanel;
    private RectTransform _buyPanelRect;
    private Text _sawCostLabel;
    private Button _buySawBtn;
    private Image _buySawBg;
    private Text _laserCostLabel;
    private Button _buyLaserBtn;
    private Image _buyLaserBg;

    // Upgrade overlay (full-screen, like GlobalUpgrades)
    private GameObject _upgradeOverlay;
    private Text _weaponNameLabel;
    private Text _sellCostLabel;
    private Button _sellBtn;

    // Dynamic upgrade rows (rebuilt when weapon changes)
    private GameObject[] _rowGOs;
    private Button[] _rowBtns;
    private Image[] _rowBgs;
    private Text[] _rowCostLabels;
    private Text[] _rowDescLabels;
    private Text[] _rowNameLabels;
    private GameObject[] _rowSoldOutLabels;

    private Stopper _currentStopper;
    private Camera _cam;
    private int _showFrame;
    private Font _font;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        _cam = Camera.main;
    }

    void Start()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildBuyPanel();
        BuildUpgradeOverlay();
        _buyPanel.SetActive(false);
        _upgradeOverlay.SetActive(false);
        Economy.Instance.OnMoneyChanged += OnMoneyChanged;
    }

    void OnDestroy()
    {
        if (Economy.Instance != null)
            Economy.Instance.OnMoneyChanged -= OnMoneyChanged;
    }

    // ═══════════════════════════════════════════════════════════════
    // Buy Panel (small popup near stopper — no weapon equipped)
    // ═══════════════════════════════════════════════════════════════

    void BuildBuyPanel()
    {
        var canvas = EconomyUI.Instance.Canvas;

        _buyPanel = new GameObject("StopperBuyPanel");
        _buyPanel.transform.SetParent(canvas.transform, false);

        _buyPanelRect = _buyPanel.AddComponent<RectTransform>();
        _buyPanelRect.sizeDelta = new Vector2(200f, 220f);

        var panelImg = _buyPanel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

        BuildBuyRow(_buyPanel.transform, "BuySawRow",
            new Vector2(10f, 115f), new Vector2(-10f, -10f),
            GameField.SawSprite(), new Color(0.75f, 0.78f, 0.82f),
            out _buySawBg, out _buySawBtn, out _sawCostLabel);
        _buySawBtn.onClick.AddListener(OnBuySawClicked);

        BuildBuyRow(_buyPanel.transform, "BuyLaserRow",
            new Vector2(10f, 10f), new Vector2(-10f, -115f),
            GameField.DishSprite(), Color.white,
            out _buyLaserBg, out _buyLaserBtn, out _laserCostLabel);
        _buyLaserBtn.onClick.AddListener(OnBuyLaserClicked);
    }

    void BuildBuyRow(Transform parent, string name,
        Vector2 offsetMin, Vector2 offsetMax,
        Sprite icon, Color iconColor,
        out Image bg, out Button btn, out Text costLabel)
    {
        var row = new GameObject(name);
        row.transform.SetParent(parent, false);

        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = Vector2.zero;
        rowRect.anchorMax = Vector2.one;
        rowRect.offsetMin = offsetMin;
        rowRect.offsetMax = offsetMax;

        bg = row.AddComponent<Image>();
        bg.color = AffordBg;
        btn = row.AddComponent<Button>();
        btn.targetGraphic = bg;

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(row.transform, false);
        var iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(10f, 0f);
        iconRect.sizeDelta = new Vector2(50f, 50f);

        var iconImg = iconGo.AddComponent<Image>();
        iconImg.sprite = icon;
        iconImg.color = iconColor;
        iconImg.raycastTarget = false;

        var costGo = new GameObject("CostLabel");
        costGo.transform.SetParent(row.transform, false);
        var costRect = costGo.AddComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0.4f, 0f);
        costRect.anchorMax = new Vector2(1f, 1f);
        costRect.offsetMin = Vector2.zero;
        costRect.offsetMax = Vector2.zero;

        costLabel = costGo.AddComponent<Text>();
        costLabel.font = _font;
        costLabel.fontSize = 32;
        costLabel.fontStyle = FontStyle.Bold;
        costLabel.alignment = TextAnchor.MiddleCenter;
        costLabel.color = Color.white;
        costLabel.raycastTarget = false;
    }

    // ═══════════════════════════════════════════════════════════════
    // Upgrade Overlay (full-screen — weapon equipped)
    // Matches GlobalUpgradesUI style exactly
    // ═══════════════════════════════════════════════════════════════

    void BuildUpgradeOverlay()
    {
        var canvas = EconomyUI.Instance.Canvas;

        // Full-screen backdrop
        _upgradeOverlay = new GameObject("WeaponUpgradeOverlay");
        _upgradeOverlay.transform.SetParent(canvas.transform, false);

        var backdropRect = _upgradeOverlay.AddComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        var backdropImg = _upgradeOverlay.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.5f);
        backdropImg.raycastTarget = true;

        // Center panel (sized dynamically in Show based on slot count)
        var panel = new GameObject("Panel");
        panel.transform.SetParent(_upgradeOverlay.transform, false);
        panel.AddComponent<RectTransform>();
        panel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // Title (weapon name — set dynamically)
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -10f);
        titleRect.sizeDelta = new Vector2(0f, 70f);

        _weaponNameLabel = titleGo.AddComponent<Text>();
        _weaponNameLabel.font = _font;
        _weaponNameLabel.fontSize = 40;
        _weaponNameLabel.fontStyle = FontStyle.Bold;
        _weaponNameLabel.alignment = TextAnchor.MiddleCenter;
        _weaponNameLabel.color = Color.white;
        _weaponNameLabel.raycastTarget = false;

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
        closeBtn.onClick.AddListener(Hide);

        var xGo = new GameObject("X");
        xGo.transform.SetParent(closeGo.transform, false);
        var xRect = xGo.AddComponent<RectTransform>();
        xRect.anchorMin = Vector2.zero;
        xRect.anchorMax = Vector2.one;
        xRect.offsetMin = Vector2.zero;
        xRect.offsetMax = Vector2.zero;
        var xText = xGo.AddComponent<Text>();
        xText.text = "X";
        xText.font = _font;
        xText.fontSize = 36;
        xText.fontStyle = FontStyle.Bold;
        xText.alignment = TextAnchor.MiddleCenter;
        xText.color = Color.white;
        xText.raycastTarget = false;

        // Sell button (bottom of panel)
        var sellGo = new GameObject("SellButton");
        sellGo.transform.SetParent(panel.transform, false);
        var sellRect = sellGo.AddComponent<RectTransform>();
        sellRect.anchorMin = new Vector2(0f, 0f);
        sellRect.anchorMax = new Vector2(1f, 0f);
        sellRect.pivot = new Vector2(0.5f, 0f);
        sellRect.anchoredPosition = new Vector2(0f, 15f);
        sellRect.sizeDelta = new Vector2(-30f, 60f);

        var sellBg = sellGo.AddComponent<Image>();
        sellBg.color = new Color(0.5f, 0.15f, 0.1f, 0.9f);
        _sellBtn = sellGo.AddComponent<Button>();
        _sellBtn.targetGraphic = sellBg;
        _sellBtn.onClick.AddListener(OnSellClicked);

        var sellLabelGo = new GameObject("SellLabel");
        sellLabelGo.transform.SetParent(sellGo.transform, false);
        var sellLabelRect = sellLabelGo.AddComponent<RectTransform>();
        sellLabelRect.anchorMin = Vector2.zero;
        sellLabelRect.anchorMax = Vector2.one;
        sellLabelRect.offsetMin = Vector2.zero;
        sellLabelRect.offsetMax = Vector2.zero;

        _sellCostLabel = sellLabelGo.AddComponent<Text>();
        _sellCostLabel.font = _font;
        _sellCostLabel.fontSize = 28;
        _sellCostLabel.fontStyle = FontStyle.Bold;
        _sellCostLabel.alignment = TextAnchor.MiddleCenter;
        _sellCostLabel.color = Color.white;
        _sellCostLabel.raycastTarget = false;
    }

    // ── Dynamic upgrade rows (built per weapon in Show) ──

    void BuildUpgradeRows(Weapon weapon)
    {
        ClearUpgradeRows();

        var panel = _upgradeOverlay.transform.Find("Panel");
        int count = weapon.UpgradeSlotCount;

        // Size the panel to fit rows
        float rowHeight = 140f;
        float rowGap = 10f;
        float topSpace = 90f;   // title
        float bottomSpace = 90f; // sell button
        float panelHeight = topSpace + count * rowHeight + (count - 1) * rowGap + bottomSpace;

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(700f, panelHeight);

        _rowGOs = new GameObject[count];
        _rowBtns = new Button[count];
        _rowBgs = new Image[count];
        _rowCostLabels = new Text[count];
        _rowDescLabels = new Text[count];
        _rowNameLabels = new Text[count];
        _rowSoldOutLabels = new GameObject[count];

        float topOffset = -topSpace;
        for (int i = 0; i < count; i++)
        {
            var info = weapon.GetSlotInfo(i);
            float yOffset = topOffset - (rowHeight + rowGap) * i;
            BuildSingleRow(panel, i, info, yOffset, rowHeight);
        }
    }

    void BuildSingleRow(Transform parent, int index, UpgradeSlotInfo info, float yOffset, float height)
    {
        var row = new GameObject(info.Name + "Row");
        row.transform.SetParent(parent, false);

        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, yOffset);
        rowRect.sizeDelta = new Vector2(-30f, height);

        var rowBg = row.AddComponent<Image>();
        rowBg.color = DarkBg;

        // Icon (left)
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(row.transform, false);
        var iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(15f, 0f);
        iconRect.sizeDelta = new Vector2(80f, 80f);

        var iconImg = iconGo.AddComponent<Image>();
        iconImg.sprite = info.Icon;
        iconImg.color = info.IconColor;
        iconImg.raycastTarget = false;

        // Sold out overlay on icon
        var soldOutGo = new GameObject("SoldOut");
        soldOutGo.transform.SetParent(iconGo.transform, false);
        var soldOutRect = soldOutGo.AddComponent<RectTransform>();
        soldOutRect.anchorMin = Vector2.zero;
        soldOutRect.anchorMax = Vector2.one;
        soldOutRect.offsetMin = Vector2.zero;
        soldOutRect.offsetMax = Vector2.zero;

        soldOutGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        var soldOutTextGo = new GameObject("Text");
        soldOutTextGo.transform.SetParent(soldOutGo.transform, false);
        var stRect = soldOutTextGo.AddComponent<RectTransform>();
        stRect.anchorMin = Vector2.zero;
        stRect.anchorMax = Vector2.one;
        stRect.offsetMin = Vector2.zero;
        stRect.offsetMax = Vector2.zero;

        var soldOutText = soldOutTextGo.AddComponent<Text>();
        soldOutText.text = "SOLD\nOUT";
        soldOutText.font = _font;
        soldOutText.fontSize = 20;
        soldOutText.fontStyle = FontStyle.Bold;
        soldOutText.alignment = TextAnchor.MiddleCenter;
        soldOutText.color = new Color(1f, 0.3f, 0.2f);
        soldOutText.raycastTarget = false;

        soldOutGo.SetActive(false);
        _rowSoldOutLabels[index] = soldOutGo;

        // Name (center-top)
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(row.transform, false);
        var nameRect = nameGo.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.5f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.offsetMin = new Vector2(110f, 0f);
        nameRect.offsetMax = new Vector2(-170f, -10f);

        var nameText = nameGo.AddComponent<Text>();
        nameText.font = _font;
        nameText.fontSize = 30;
        nameText.fontStyle = FontStyle.Bold;
        nameText.alignment = TextAnchor.MiddleLeft;
        nameText.color = Color.white;
        nameText.raycastTarget = false;
        _rowNameLabels[index] = nameText;

        // Description (center-bottom)
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
        _rowDescLabels[index] = descText;

        // Buy button (right)
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
        _rowBgs[index] = buyBg;

        var buyBtn = buyGo.AddComponent<Button>();
        buyBtn.targetGraphic = buyBg;
        _rowBtns[index] = buyBtn;

        int slot = index;
        buyBtn.onClick.AddListener(() => OnUpgradeClicked(slot));

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
        _rowCostLabels[index] = costText;

        _rowGOs[index] = row;
    }

    void ClearUpgradeRows()
    {
        if (_rowGOs == null) return;
        foreach (var go in _rowGOs)
            if (go != null) Destroy(go);
        _rowGOs = null;
    }

    // ═══════════════════════════════════════════════════════════════
    // Show / Hide
    // ═══════════════════════════════════════════════════════════════

    public void Show(Stopper stopper)
    {
        _currentStopper = stopper;

        if (stopper.HasWeapon)
        {
            _buyPanel.SetActive(false);
            _weaponNameLabel.text = stopper.Weapon.DisplayName.ToUpper();
            BuildUpgradeRows(stopper.Weapon);
            RefreshUpgradeRows();
            RefreshSellButton();
            _upgradeOverlay.SetActive(true);
        }
        else
        {
            _upgradeOverlay.SetActive(false);

            Vector3 screenPos = _cam.WorldToScreenPoint(stopper.transform.position);
            var canvasRect = EconomyUI.Instance.Canvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, null, out Vector2 localPos);
            _buyPanelRect.anchoredPosition = localPos + new Vector2(0f, 140f);

            RefreshBuyButtons();
            _buyPanel.SetActive(true);
        }

        _showFrame = Time.frameCount;
    }

    public void Hide()
    {
        ClearUpgradeRows();
        _buyPanel.SetActive(false);
        _upgradeOverlay.SetActive(false);
        _currentStopper = null;
    }

    // ═══════════════════════════════════════════════════════════════
    // Actions
    // ═══════════════════════════════════════════════════════════════

    void OnBuySawClicked()
    {
        if (_currentStopper == null) return;
        if (Economy.Instance.TryBuySaw())
        {
            StopperFactory.Instance.AttachSaw(_currentStopper);
            Hide();
        }
    }

    void OnBuyLaserClicked()
    {
        if (_currentStopper == null) return;
        if (Economy.Instance.TryBuyLaser())
        {
            StopperFactory.Instance.AttachLaser(_currentStopper);
            Hide();
        }
    }

    void OnUpgradeClicked(int slot)
    {
        if (_currentStopper == null || !_currentStopper.HasWeapon) return;
        if (_currentStopper.Weapon.TryUpgrade(slot))
        {
            RefreshUpgradeRows();
            RefreshSellButton();
        }
    }

    void OnSellClicked()
    {
        if (_currentStopper == null || !_currentStopper.HasWeapon) return;
        Economy.Instance.SellWeapon(_currentStopper.Weapon);
        StopperFactory.Instance.DetachWeapon(_currentStopper);
        Hide();
    }

    // ═══════════════════════════════════════════════════════════════
    // Refresh
    // ═══════════════════════════════════════════════════════════════

    void OnMoneyChanged(int money)
    {
        if (_buyPanel.activeSelf)
            RefreshBuyButtons();
        if (_upgradeOverlay.activeSelf)
            RefreshUpgradeRows();
    }

    void RefreshBuyButtons()
    {
        int sawCost = Economy.Instance.SawCost;
        _sawCostLabel.text = "$" + sawCost;
        bool canAffordSaw = Economy.Instance.CanAffordSaw();
        _buySawBg.color = canAffordSaw ? AffordBg : CantAffordBg;
        _buySawBtn.interactable = canAffordSaw;

        int laserCost = Economy.Instance.LaserCost;
        _laserCostLabel.text = "$" + laserCost;
        bool canAffordLaser = Economy.Instance.CanAffordLaser();
        _buyLaserBg.color = canAffordLaser ? AffordBg : CantAffordBg;
        _buyLaserBtn.interactable = canAffordLaser;
    }

    void RefreshUpgradeRows()
    {
        if (_rowBtns == null || _currentStopper == null || !_currentStopper.HasWeapon) return;
        var weapon = _currentStopper.Weapon;
        int money = Economy.Instance.Money;

        for (int i = 0; i < weapon.UpgradeSlotCount; i++)
        {
            var info = weapon.GetSlotInfo(i);
            _rowNameLabels[i].text = info.Name;
            _rowDescLabels[i].text = info.Description;
            _rowSoldOutLabels[i].SetActive(info.IsMaxed);

            if (info.IsMaxed)
            {
                _rowBtns[i].gameObject.SetActive(false);
            }
            else
            {
                _rowBtns[i].gameObject.SetActive(true);
                _rowCostLabels[i].text = "$" + info.Cost;

                bool canAfford = money >= info.Cost;
                _rowBgs[i].color = canAfford ? AffordBg : CantAffordBg;
                _rowBtns[i].interactable = canAfford;
            }
        }
    }

    void RefreshSellButton()
    {
        if (_currentStopper == null || !_currentStopper.HasWeapon) return;
        int refund = Economy.Instance.WeaponSellPrice(_currentStopper.Weapon);
        _sellCostLabel.text = "Sell $" + refund;
    }

    // ── Click outside to close buy panel ──

    void LateUpdate()
    {
        if (_buyPanel == null || !_buyPanel.activeSelf) return;
        if (Time.frameCount <= _showFrame) return;

        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = mouse.position.ReadValue();
            if (!RectTransformUtility.RectangleContainsScreenPoint(_buyPanelRect, mousePos))
                Hide();
        }
    }
}
