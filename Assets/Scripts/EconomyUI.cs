using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Creates the HUD canvas with money display (top-center) and
/// a buy-stopper button (top-left) with icon and cost label.
/// </summary>
public class EconomyUI : MonoBehaviour
{
    public static EconomyUI Instance { get; private set; }
    public Canvas Canvas { get; private set; }

    private Text _moneyLabel;
    private Text _stopperCostLabel;
    private Button _buyStopperBtn;
    private Image _buyStopperBg;
    private Text _savedIndicator;
    private float _savedFadeTimer;
    private GameObject _optionsOverlay;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        BuildCanvas();
        BuildMoneyLabel();
        BuildBuyStopperButton();
        BuildOptionsButton();
        BuildOptionsOverlay();
    }

    void Start()
    {
        Economy.Instance.OnMoneyChanged += RefreshUI;
        RefreshUI(Economy.Instance.Money);

        if (SaveManager.Instance != null)
            SaveManager.Instance.OnSaved += ShowSavedIndicator;
    }

    void OnDestroy()
    {
        if (Economy.Instance != null)
            Economy.Instance.OnMoneyChanged -= RefreshUI;
        if (SaveManager.Instance != null)
            SaveManager.Instance.OnSaved -= ShowSavedIndicator;
    }

    void Update()
    {
        if (_savedFadeTimer > 0f)
        {
            _savedFadeTimer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(_savedFadeTimer / 0.5f);
            if (_savedIndicator != null)
            {
                var c = _savedIndicator.color;
                c.a = alpha;
                _savedIndicator.color = c;
            }
            if (_savedFadeTimer <= 0f && _savedIndicator != null)
                _savedIndicator.gameObject.SetActive(false);
        }
    }

    void BuildCanvas()
    {
        var canvasGo = new GameObject("EconomyCanvas");
        Canvas = canvasGo.AddComponent<Canvas>();
        Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Canvas.sortingOrder = 100;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // EventSystem is required for UI interaction
        if (EventSystem.current == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }
    }

    void BuildMoneyLabel()
    {
        var go = new GameObject("MoneyLabel");
        go.transform.SetParent(Canvas.transform, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -20f);
        rect.sizeDelta = new Vector2(300f, 80f);

        _moneyLabel = go.AddComponent<Text>();
        _moneyLabel.text = "$10";
        _moneyLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _moneyLabel.fontSize = 52;
        _moneyLabel.fontStyle = FontStyle.Bold;
        _moneyLabel.alignment = TextAnchor.MiddleCenter;
        _moneyLabel.color = new Color(0.2f, 1f, 0.3f); // green money color

        // Drop shadow via outline
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    void BuildBuyStopperButton()
    {
        // Button container
        var go = new GameObject("BuyStopperButton");
        go.transform.SetParent(Canvas.transform, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -20f);
        rect.sizeDelta = new Vector2(140f, 140f);

        // Background panel
        _buyStopperBg = go.AddComponent<Image>();
        _buyStopperBg.color = new Color(0.15f, 0.15f, 0.2f, 0.85f);

        _buyStopperBtn = go.AddComponent<Button>();
        _buyStopperBtn.targetGraphic = _buyStopperBg;
        _buyStopperBtn.onClick.AddListener(OnBuyStopperClicked);

        // Stopper icon (circle)
        var iconGo = new GameObject("StopperIcon");
        iconGo.transform.SetParent(go.transform, false);

        var iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.6f);
        iconRect.anchorMax = new Vector2(0.5f, 0.6f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(60f, 60f);

        var iconImg = iconGo.AddComponent<Image>();
        iconImg.sprite = GameField.CircleSprite();
        iconImg.color = new Color(0.35f, 0.35f, 0.4f);
        iconImg.raycastTarget = false;

        // Cost label
        var costGo = new GameObject("CostLabel");
        costGo.transform.SetParent(go.transform, false);

        var costRect = costGo.AddComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0f, 0f);
        costRect.anchorMax = new Vector2(1f, 0.35f);
        costRect.offsetMin = Vector2.zero;
        costRect.offsetMax = Vector2.zero;

        _stopperCostLabel = costGo.AddComponent<Text>();
        _stopperCostLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _stopperCostLabel.fontSize = 30;
        _stopperCostLabel.fontStyle = FontStyle.Bold;
        _stopperCostLabel.alignment = TextAnchor.MiddleCenter;
        _stopperCostLabel.color = Color.white;
        _stopperCostLabel.raycastTarget = false;
    }

    void BuildOptionsButton()
    {
        var go = new GameObject("OptionsButton");
        go.transform.SetParent(Canvas.transform, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(20f, 20f);
        rect.sizeDelta = new Vector2(80f, 80f);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.2f, 0.85f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(ToggleOptionsOverlay);

        // Gear-style icon (circle)
        var iconGo = new GameObject("OptionsIcon");
        iconGo.transform.SetParent(go.transform, false);

        var iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.15f, 0.3f);
        iconRect.anchorMax = new Vector2(0.85f, 0.85f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        var iconImg = iconGo.AddComponent<Image>();
        iconImg.sprite = GameField.CircleSprite();
        iconImg.color = new Color(0.6f, 0.6f, 0.65f);
        iconImg.raycastTarget = false;

        // Label below icon
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);

        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0.3f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelGo.AddComponent<Text>();
        label.text = "Options";
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 16;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    void BuildOptionsOverlay()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Full-screen backdrop
        _optionsOverlay = new GameObject("OptionsOverlay");
        _optionsOverlay.transform.SetParent(Canvas.transform, false);

        var backdropRect = _optionsOverlay.AddComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        var backdropImg = _optionsOverlay.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.5f);
        backdropImg.raycastTarget = true;

        // Center panel
        var panel = new GameObject("Panel");
        panel.transform.SetParent(_optionsOverlay.transform, false);

        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(500f, 400f);

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
        titleText.text = "OPTIONS";
        titleText.font = font;
        titleText.fontSize = 40;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.raycastTarget = false;

        // Close button (top-right X)
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
        closeBtn.onClick.AddListener(() => _optionsOverlay.SetActive(false));

        var xLabelGo = new GameObject("XLabel");
        xLabelGo.transform.SetParent(closeGo.transform, false);

        var xRect = xLabelGo.AddComponent<RectTransform>();
        xRect.anchorMin = Vector2.zero;
        xRect.anchorMax = Vector2.one;
        xRect.offsetMin = Vector2.zero;
        xRect.offsetMax = Vector2.zero;

        var xLabel = xLabelGo.AddComponent<Text>();
        xLabel.text = "X";
        xLabel.font = font;
        xLabel.fontSize = 36;
        xLabel.fontStyle = FontStyle.Bold;
        xLabel.alignment = TextAnchor.MiddleCenter;
        xLabel.color = Color.white;
        xLabel.raycastTarget = false;

        // Save button
        BuildOverlayButton(panel.transform, "SaveButton", "Save Game",
            new Vector2(0f, -100f), new Color(0.2f, 0.5f, 0.8f, 0.9f), font, OnSaveClicked);

        // Saved indicator (inside panel, below save button)
        var savedGo = new GameObject("SavedIndicator");
        savedGo.transform.SetParent(panel.transform, false);

        var savedRect = savedGo.AddComponent<RectTransform>();
        savedRect.anchorMin = new Vector2(0.5f, 1f);
        savedRect.anchorMax = new Vector2(0.5f, 1f);
        savedRect.pivot = new Vector2(0.5f, 1f);
        savedRect.anchoredPosition = new Vector2(0f, -180f);
        savedRect.sizeDelta = new Vector2(200f, 40f);

        _savedIndicator = savedGo.AddComponent<Text>();
        _savedIndicator.text = "Saved!";
        _savedIndicator.font = font;
        _savedIndicator.fontSize = 28;
        _savedIndicator.fontStyle = FontStyle.Bold;
        _savedIndicator.alignment = TextAnchor.MiddleCenter;
        _savedIndicator.color = new Color(0.5f, 1f, 0.5f, 0f);
        savedGo.SetActive(false);

        // Restart button
        BuildOverlayButton(panel.transform, "RestartButton", "Restart",
            new Vector2(0f, -240f), new Color(0.7f, 0.2f, 0.15f, 0.9f), font, OnRestartClicked);

        _optionsOverlay.SetActive(false);
    }

    void BuildOverlayButton(Transform parent, string name, string label,
        Vector2 position, Color bgColor, Font font, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(400f, 70f);

        var bg = go.AddComponent<Image>();
        bg.color = bgColor;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(onClick);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);

        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var labelText = labelGo.AddComponent<Text>();
        labelText.text = label;
        labelText.font = font;
        labelText.fontSize = 30;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;
        labelText.raycastTarget = false;
    }

    void ToggleOptionsOverlay()
    {
        _optionsOverlay.SetActive(!_optionsOverlay.activeSelf);
    }

    void OnSaveClicked()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.Save();
    }

    void OnRestartClicked()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.DeleteSave();
        SceneManager.LoadScene(0);
    }

    void ShowSavedIndicator()
    {
        if (_savedIndicator == null) return;
        _savedIndicator.gameObject.SetActive(true);
        _savedIndicator.color = new Color(0.5f, 1f, 0.5f, 1f);
        _savedFadeTimer = 1.5f;
    }

    void OnBuyStopperClicked()
    {
        if (Economy.Instance.TryBuyStopper())
        {
            var pos = StopperFactory.Instance.FindClearSpawnPos(new Vector2(0f, 1f));
            StopperFactory.Instance.SpawnStopper(pos);
        }
    }

    void RefreshUI(int money)
    {
        _moneyLabel.text = "$" + money;

        int cost = Economy.Instance.StopperCost;
        _stopperCostLabel.text = "$" + cost;

        bool canAfford = Economy.Instance.CanAffordStopper();
        _buyStopperBg.color = canAfford
            ? new Color(0.15f, 0.15f, 0.2f, 0.85f)
            : new Color(0.3f, 0.1f, 0.1f, 0.6f);
        _buyStopperBtn.interactable = canAfford;
    }
}
