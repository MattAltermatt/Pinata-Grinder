using UnityEngine;

/// <summary>
/// Weapon group that manages a single BlackHole instance on a stopper.
/// Implements 5 upgrade slots: Pull Range, Pull Force, Core Size, Core Damage, Multi-Pull.
/// </summary>
public class BlackHoleGroup : Weapon
{
    private const int SlotPullRange  = 0;
    private const int SlotPullForce  = 1;
    private const int SlotCoreSize   = 2;
    private const int SlotCoreDamage = 3;
    private const int SlotMultiPull  = 4;
    private const int TotalSlots     = 5;

    private static readonly int[] BaseCosts = { 8, 5, 8, 10, 12 };
    private static readonly int[] MaxLevels = { 20, 20, 20, 0, 10 };

    private const float StartPullRange = 0.4f;
    private const float MaxPullRange   = 1.0f;
    private const float StartPullForce = 15f;
    private const float MaxPullForce   = 80f;
    private const float StartCoreSize  = 0.3f;
    private const float MaxCoreSize    = 1.5f;
    private const float StartCoreDPS   = 1f;
    private const int   StartTargets   = 1;

    private BlackHole _blackHole;
    private WeaponUpgradeData _upgrades;
    private Transform _stopper;
    private float _stopperRadius;

    public override WeaponType Type => WeaponType.BlackHole;
    public override string DisplayName => "Black Hole";
    public override WeaponUpgradeData Upgrades => _upgrades;
    public override int UpgradeSlotCount => TotalSlots;

    public override void Init(Vector2 stopperCenter, float stopperRadius)
    {
        _stopperRadius = stopperRadius;
        _upgrades = new WeaponUpgradeData(TotalSlots);
        CreateBlackHole();
    }

    public void SetStopper(Transform stopperTransform)
    {
        _stopper = stopperTransform;
        if (_blackHole != null)
            _blackHole.SetStopper(stopperTransform);

        // Hide stopper sprite — black hole replaces it visually
        var sr = stopperTransform.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
    }

    // ── Upgrade logic ──

    public override bool TryUpgrade(int slot)
    {
        int maxLvl = MaxLevels[slot];
        int cost = _upgrades.UpgradeCost(slot, BaseCosts[slot]);

        if (IsDebugMode)
        {
            _upgrades.BuyUpgrade(slot, 0);
            ApplyUpgrade(slot);
            return true;
        }

        if (maxLvl > 0 && _upgrades.GetLevel(slot) >= maxLvl) return false;
        if (Economy.Instance == null || Economy.Instance.Money < cost) return false;
        Economy.Instance.Earn(-cost);

        _upgrades.BuyUpgrade(slot, cost);
        ApplyUpgrade(slot);
        return true;
    }

    void ApplyUpgrade(int slot)
    {
        switch (slot)
        {
            case SlotPullRange:  ApplyPullRange(); break;
            case SlotPullForce:  ApplyPullForce(); break;
            case SlotCoreSize:   ApplyCoreSize(); break;
            case SlotCoreDamage: ApplyCoreDamage(); break;
            case SlotMultiPull:  ApplyMultiPull(); break;
        }
    }

    // ── Slot calculations ──

    float CurrentPullRange()
    {
        int lvl = _upgrades.GetLevel(SlotPullRange);
        return StartPullRange + lvl * ((MaxPullRange - StartPullRange) / MaxLevels[SlotPullRange]);
    }

    float CurrentPullForce()
    {
        int lvl = _upgrades.GetLevel(SlotPullForce);
        return StartPullForce + lvl * ((MaxPullForce - StartPullForce) / MaxLevels[SlotPullForce]);
    }

    float CurrentCoreSize()
    {
        int lvl = _upgrades.GetLevel(SlotCoreSize);
        return StartCoreSize + lvl * ((MaxCoreSize - StartCoreSize) / MaxLevels[SlotCoreSize]);
    }

    float CurrentCoreDPS()
    {
        int lvl = _upgrades.GetLevel(SlotCoreDamage);
        return StartCoreDPS + lvl * 0.5f;
    }

    int CurrentMaxTargets()
    {
        int lvl = _upgrades.GetLevel(SlotMultiPull);
        return StartTargets + lvl;
    }

    // ── Apply to black hole ──

    void ApplyPullRange()
    {
        if (_blackHole != null) _blackHole.SetPullRange(CurrentPullRange());
    }

    void ApplyPullForce()
    {
        if (_blackHole != null) _blackHole.SetPullForce(CurrentPullForce());
    }

    void ApplyCoreSize()
    {
        if (_blackHole != null) _blackHole.SetCoreRadius(CurrentCoreSize());
    }

    void ApplyCoreDamage()
    {
        if (_blackHole != null) _blackHole.SetCoreDPS(CurrentCoreDPS());
    }

    void ApplyMultiPull()
    {
        if (_blackHole != null) _blackHole.SetMaxTargets(CurrentMaxTargets());
    }

    // ── Create black hole ──

    void CreateBlackHole()
    {
        var go = new GameObject("BlackHole");
        _blackHole = go.AddComponent<BlackHole>();
        _blackHole.Init(
            _stopper != null ? (Vector2)_stopper.position : Vector2.zero,
            _stopperRadius
        );
        _blackHole.SetPullRange(CurrentPullRange());
        _blackHole.SetPullForce(CurrentPullForce());
        _blackHole.SetCoreRadius(CurrentCoreSize());
        _blackHole.SetCoreDPS(CurrentCoreDPS());
        _blackHole.SetMaxTargets(CurrentMaxTargets());

        if (_stopper != null)
            _blackHole.SetStopper(_stopper);
    }

    // ── Restore from save ──

    public void RestoreUpgrades(int[] levels, int totalInvestment)
    {
        if (levels == null) return;
        _upgrades.RestoreState(levels, totalInvestment);

        ApplyPullRange();
        ApplyPullForce();
        ApplyCoreSize();
        ApplyCoreDamage();
        ApplyMultiPull();
    }

    // ── Cleanup ──

    void OnDestroy()
    {
        if (_blackHole != null)
            Destroy(_blackHole.gameObject);
    }

    // ── Slot info for UI ──

    public override UpgradeSlotInfo GetSlotInfo(int slot)
    {
        int lvl = _upgrades.GetLevel(slot);
        int maxLvl = MaxLevels[slot];
        bool maxed = maxLvl > 0 && lvl >= maxLvl;
        int cost = _upgrades.UpgradeCost(slot, BaseCosts[slot]);

        return slot switch
        {
            SlotPullRange => new UpgradeSlotInfo
            {
                Name = "Range",
                Description = CurrentPullRange().ToString("F1") + " units",
                Icon = GameField.CrosshairSprite(64),
                IconColor = new Color(0.3f, 0.9f, 0.9f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            SlotPullForce => new UpgradeSlotInfo
            {
                Name = "Pull",
                Description = "Force: " + CurrentPullForce().ToString("F1"),
                Icon = GameField.WaveSprite(64),
                IconColor = new Color(0.6f, 0.2f, 0.9f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            SlotCoreSize => new UpgradeSlotInfo
            {
                Name = "Core",
                Description = CurrentCoreSize().ToString("F2") + " radius",
                Icon = GameField.CircleSprite(64),
                IconColor = new Color(1f, 0.6f, 0.2f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            SlotCoreDamage => new UpgradeSlotInfo
            {
                Name = "Damage",
                Description = "DPS: " + CurrentCoreDPS().ToString("F1"),
                Icon = GameField.BoltSprite(64),
                IconColor = new Color(1f, 0.3f, 0.3f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            SlotMultiPull => new UpgradeSlotInfo
            {
                Name = "Targets",
                Description = "Max: " + CurrentMaxTargets(),
                Icon = GameField.GridSprite(64),
                IconColor = new Color(0.3f, 0.8f, 0.7f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            _ => default
        };
    }
}
