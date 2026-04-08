using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        BuildCanvas();
        BuildMoneyLabel();
        BuildBuyStopperButton();
    }

    void Start()
    {
        Economy.Instance.OnMoneyChanged += RefreshUI;
        RefreshUI(Economy.Instance.Money);
    }

    void OnDestroy()
    {
        if (Economy.Instance != null)
            Economy.Instance.OnMoneyChanged -= RefreshUI;
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
