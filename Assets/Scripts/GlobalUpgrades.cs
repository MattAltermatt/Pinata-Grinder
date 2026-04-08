using System;
using UnityEngine;

/// <summary>
/// Singleton that manages global upgrade levels and applies their effects.
/// Pricing follows the same exponential formula as the rest of the economy.
/// Spawner rate and oscillation rate are capped at 20 levels.
/// Wall size is capped at the player's screen width minus a UI margin.
/// </summary>
public class GlobalUpgrades : MonoBehaviour
{
    public static GlobalUpgrades Instance { get; private set; }

    private const int BaseCost = 8;
    private const float CostMultiplier = 1.6f;
    private const int MaxSpawnerLevel = 20;
    private const int MaxOscillationLevel = 20;
    private const float MinFieldWidth = 1.5f;
    private const float WallStep = 0.5f;

    // Spawner rate: 7s at level 0, 0.1s at level 20
    private const float SpawnIntervalStart = 7f;
    private const float SpawnIntervalEnd = 0.1f;

    // Oscillation: period 20s at level 0, 1s at level 20
    private const float OscPeriodStart = 20f;
    private const float OscPeriodEnd = 1f;

    private int _wallLevel;
    private int _pinataLevel;
    private int _spawnerLevel;
    private int _oscillationLevel;
    private int _healthLevel;
    private int _deathLineDamageLevel;

    private SquareSpawner _spawner;

    public int WallLevel => _wallLevel;
    public int PinataLevel => _pinataLevel;
    public int SpawnerLevel => _spawnerLevel;
    public int OscillationLevel => _oscillationLevel;
    public int HealthLevel => _healthLevel;
    public int DeathLineDamageLevel => _deathLineDamageLevel;

    public event Action OnUpgradeChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        _spawner = GetComponent<SquareSpawner>();
        ApplyAll();
    }

    void EnsureSpawner()
    {
        if (_spawner == null)
            _spawner = GetComponent<SquareSpawner>();
    }

    // ── Pricing ──

    public int WallCost => Cost(_wallLevel);
    public int PinataCost => Cost(_pinataLevel);
    public int SpawnerCost => Cost(_spawnerLevel);
    public int OscillationCost => Cost(_oscillationLevel);
    public int HealthCost => Cost(_healthLevel);
    public int DeathLineDamageCost => Cost(_deathLineDamageLevel);

    static int Cost(int level) => Mathf.RoundToInt(BaseCost * Mathf.Pow(CostMultiplier, level));

    // ── Max checks ──

    public bool IsWallMaxed => CalculateFieldWidth(_wallLevel) >= MaxFieldWidth();
    public bool IsPinataMaxed => !NextPinataFits();
    public bool IsSpawnerMaxed => _spawnerLevel >= MaxSpawnerLevel;
    public bool IsOscillationMaxed => _oscillationLevel >= MaxOscillationLevel;

    // ── Buy methods ──

    bool TrySpend(int cost)
    {
        if (Economy.Instance.Money < cost) return false;
        Economy.Instance.Earn(-cost);
        return true;
    }

    public bool TryBuyWall()
    {
        if (IsWallMaxed) return false;
        if (!TrySpend(WallCost)) return false;
        _wallLevel++;
        ApplyWallSize();
        OnUpgradeChanged?.Invoke();
        return true;
    }

    public bool TryBuyPinata()
    {
        if (IsPinataMaxed) return false;
        if (!TrySpend(PinataCost)) return false;
        _pinataLevel++;
        ApplyPinataSize();
        OnUpgradeChanged?.Invoke();
        return true;
    }

    public bool TryBuySpawner()
    {
        if (IsSpawnerMaxed) return false;
        if (!TrySpend(SpawnerCost)) return false;
        _spawnerLevel++;
        ApplySpawnerRate();
        OnUpgradeChanged?.Invoke();
        return true;
    }

    public bool TryBuyOscillation()
    {
        if (IsOscillationMaxed) return false;
        if (!TrySpend(OscillationCost)) return false;
        _oscillationLevel++;
        ApplyOscillationRate();
        OnUpgradeChanged?.Invoke();
        return true;
    }

    public bool TryBuyHealth()
    {
        if (!TrySpend(HealthCost)) return false;
        _healthLevel++;
        ApplyHealth();
        OnUpgradeChanged?.Invoke();
        return true;
    }

    public bool TryBuyDeathLineDamage()
    {
        if (!TrySpend(DeathLineDamageCost)) return false;
        _deathLineDamageLevel++;
        ApplyDeathLineDamage();
        OnUpgradeChanged?.Invoke();
        return true;
    }

    // ── Apply all (called once at start) ──

    void ApplyAll()
    {
        ApplyWallSize();
        ApplyPinataSize();
        ApplySpawnerRate();
        ApplyOscillationRate();
        ApplyHealth();
        ApplyDeathLineDamage();
    }

    // ── Apply individual upgrades ──

    void ApplyWallSize()
    {
        float width = CalculateFieldWidth(_wallLevel);
        GameField.Instance.RebuildWalls(width);
        StopperFactory.Instance.UpdateFieldWidth(width);
        if (_spawner != null) _spawner.SetFieldWidth(width);
    }

    void ApplyPinataSize()
    {
        if (_spawner == null) return;
        _spawner.SetSquareCount(GetSquareCount(_pinataLevel));
    }

    void ApplySpawnerRate()
    {
        if (_spawner == null) return;
        _spawner.SetSpawnInterval(CalculateSpawnInterval(_spawnerLevel));
    }

    void ApplyOscillationRate()
    {
        if (_spawner == null) return;
        _spawner.SetOscillateSpeed(CalculateOscillateSpeed(_oscillationLevel));
    }

    void ApplyHealth()
    {
        if (_spawner == null) return;
        _spawner.SetSquareHealth(CalculateHealth(_healthLevel));
    }

    void ApplyDeathLineDamage()
    {
        // DeathLine reads the damage value directly from GlobalUpgrades.Instance
        // No explicit apply step needed — just fire the event so UI refreshes
    }

    // ── Fit check ──

    bool NextPinataFits()
    {
        int nextCount = GetSquareCount(_pinataLevel + 1);
        float squareSize = _spawner != null ? _spawner.SquareSize : 0.175f;
        // Estimate worst-case bounding box as a square arrangement
        float side = Mathf.Ceil(Mathf.Sqrt(nextCount)) * squareSize;
        float currentFieldWidth = CalculateFieldWidth(_wallLevel);
        float fieldHeight = GameField.Instance != null ? GameField.Instance.CameraHalfHeight * 2f : 10f;
        return side < currentFieldWidth && side < fieldHeight;
    }

    // ── Calculation helpers (public for UI display) ──

    public float CalculateFieldWidth(int level)
    {
        float width = MinFieldWidth + level * WallStep;
        return Mathf.Min(width, MaxFieldWidth());
    }

    public float MaxFieldWidth()
    {
        var cam = Camera.main;
        if (cam == null) return 15f;
        return cam.aspect * GameField.Instance.CameraHalfHeight * 2f - 1.5f;
    }

    public static int GetSquareCount(int level)
    {
        return level + 1;
    }

    public static void GetGridSize(int level, out int w, out int h)
    {
        int n = level / 2 + 1;
        h = n;
        w = (level % 2 == 0) ? n : n + 1;
    }

    public float CalculateSpawnInterval(int level)
    {
        if (level >= MaxSpawnerLevel) return SpawnIntervalEnd;
        return SpawnIntervalStart * Mathf.Pow(SpawnIntervalEnd / SpawnIntervalStart, (float)level / MaxSpawnerLevel);
    }

    public float CalculateOscillationPeriod(int level)
    {
        if (level >= MaxOscillationLevel) return OscPeriodEnd;
        return OscPeriodStart * Mathf.Pow(OscPeriodEnd / OscPeriodStart, (float)level / MaxOscillationLevel);
    }

    float CalculateOscillateSpeed(int level)
    {
        float period = CalculateOscillationPeriod(level);
        return 2f * Mathf.PI / period;
    }

    /// <summary>
    /// Health grows at an accelerating rate: 1 + 0.3 * level^1.5
    /// Level 0: 1 HP, Level 5: ~4.4, Level 10: ~10.5, Level 20: ~27.8
    /// </summary>
    public float CalculateHealth(int level)
    {
        return 1f + 0.3f * Mathf.Pow(level, 1.5f);
    }

    /// <summary>
    /// Death line damage grows linearly: 1 + level * 0.5
    /// Level 0: 1, Level 5: 3.5, Level 10: 6, Level 20: 11
    /// Helps with high-health pinatas but doesn't fully solve them.
    /// </summary>
    public float CalculateDeathLineDamage(int level)
    {
        return 1f + level * 0.5f;
    }

    /// <summary>Current death line damage value for the active level.</summary>
    public float DeathLineDamage => CalculateDeathLineDamage(_deathLineDamageLevel);

    public GlobalUpgradesSaveData CaptureState()
    {
        return new GlobalUpgradesSaveData
        {
            wallLevel = _wallLevel,
            pinataLevel = _pinataLevel,
            spawnerLevel = _spawnerLevel,
            oscillationLevel = _oscillationLevel,
            healthLevel = _healthLevel,
            deathLineDamageLevel = _deathLineDamageLevel
        };
    }

    public void RestoreState(GlobalUpgradesSaveData data)
    {
        if (data == null) return;
        _wallLevel = data.wallLevel;
        _pinataLevel = data.pinataLevel;
        _spawnerLevel = data.spawnerLevel;
        _oscillationLevel = data.oscillationLevel;
        _healthLevel = data.healthLevel;
        _deathLineDamageLevel = data.deathLineDamageLevel;

        // Apply immediately if spawner is already available (Start may have run)
        EnsureSpawner();
        if (_spawner != null)
            ApplyAll();
    }
}
