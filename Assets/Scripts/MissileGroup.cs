using UnityEngine;

/// <summary>
/// Weapon group that manages a single MissileLauncher instance on a stopper.
/// Implements 5 upgrade slots: Fire Rate, Damage, Blast Radius, Missile Speed, Homing.
/// </summary>
public class MissileGroup : Weapon
{
    private const int SlotFireRate    = 0;
    private const int SlotDamage      = 1;
    private const int SlotBlastRadius = 2;
    private const int SlotSpeed       = 3;
    private const int SlotHoming      = 4;
    private const int SlotMinRange    = 5;
    private const int SlotMaxRange    = 6;
    private const int TotalSlots      = 7;

    private static readonly int[] BaseCosts = { 8, 10, 8, 5, 12, 5, 8 };
    private static readonly int[] MaxLevels = { 20, 0, 20, 20, 10, 20, 20 };

    private const float StartFireRate    = 5f;
    private const float MinFireRate      = 0.5f;
    private const float StartDamage      = 2f;
    private const float DamagePerLevel   = 1.5f;
    private const float StartBlastRadius = 0.3f;
    private const float MaxBlastRadius   = 1.5f;
    private const float StartSpeed       = 1.5f;
    private const float MaxSpeed         = 6f;
    private const float StartHoming      = 0f;
    private const float MaxHoming        = 5f;
    private const float StartMinRange    = 1.5f;
    private const float FinalMinRange    = 0.3f;
    private const float StartMaxRange    = 4f;
    private const float FinalMaxRange    = 15f;

    private MissileLauncher _launcher;
    private WeaponUpgradeData _upgrades;
    private Transform _stopper;
    private float _stopperRadius;

    public override WeaponType Type => WeaponType.Missile;
    public override string DisplayName => "Missile";
    public override WeaponUpgradeData Upgrades => _upgrades;
    public override int UpgradeSlotCount => TotalSlots;

    public override void Init(Vector2 stopperCenter, float stopperRadius)
    {
        _stopperRadius = stopperRadius;
        _upgrades = new WeaponUpgradeData(TotalSlots);
        CreateLauncher();
    }

    public void SetStopper(Transform stopperTransform)
    {
        _stopper = stopperTransform;
        if (_launcher != null)
            _launcher.SetStopper(stopperTransform);

        // Hide stopper sprite — launcher replaces it visually
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
            case SlotFireRate:    ApplyFireRate(); break;
            case SlotDamage:      ApplyDamage(); break;
            case SlotBlastRadius: ApplyBlastRadius(); break;
            case SlotSpeed:       ApplySpeed(); break;
            case SlotHoming:      ApplyHoming(); break;
            case SlotMinRange:    ApplyMinRange(); break;
            case SlotMaxRange:    ApplyMaxRange(); break;
        }
    }

    // ── Stat calculations ──

    float CurrentFireInterval()
    {
        int lvl = _upgrades.GetLevel(SlotFireRate);
        return StartFireRate - lvl * ((StartFireRate - MinFireRate) / MaxLevels[SlotFireRate]);
    }

    float CurrentDamage()
    {
        int lvl = _upgrades.GetLevel(SlotDamage);
        return StartDamage + lvl * DamagePerLevel;
    }

    float CurrentBlastRadius()
    {
        int lvl = _upgrades.GetLevel(SlotBlastRadius);
        return StartBlastRadius + lvl * ((MaxBlastRadius - StartBlastRadius) / MaxLevels[SlotBlastRadius]);
    }

    float CurrentSpeed()
    {
        int lvl = _upgrades.GetLevel(SlotSpeed);
        return StartSpeed + lvl * ((MaxSpeed - StartSpeed) / MaxLevels[SlotSpeed]);
    }

    float CurrentHoming()
    {
        int lvl = _upgrades.GetLevel(SlotHoming);
        return StartHoming + lvl * ((MaxHoming - StartHoming) / MaxLevels[SlotHoming]);
    }

    float CurrentMinRange()
    {
        int lvl = _upgrades.GetLevel(SlotMinRange);
        // Min range decreases with upgrades (shrinks the dead zone)
        return StartMinRange - lvl * ((StartMinRange - FinalMinRange) / MaxLevels[SlotMinRange]);
    }

    float CurrentMaxRange()
    {
        int lvl = _upgrades.GetLevel(SlotMaxRange);
        return StartMaxRange + lvl * ((FinalMaxRange - StartMaxRange) / MaxLevels[SlotMaxRange]);
    }

    // ── Apply to all launchers ──

    void ApplyFireRate()
    {
        if (_launcher != null) _launcher.SetFireInterval(CurrentFireInterval());
    }

    void ApplyDamage()
    {
        if (_launcher != null) _launcher.SetDamage(CurrentDamage());
    }

    void ApplyBlastRadius()
    {
        if (_launcher != null) _launcher.SetBlastRadius(CurrentBlastRadius());
    }

    void ApplySpeed()
    {
        if (_launcher != null) _launcher.SetMissileSpeed(CurrentSpeed());
    }

    void ApplyHoming()
    {
        if (_launcher != null) _launcher.SetHomingStrength(CurrentHoming());
    }

    void ApplyMinRange()
    {
        if (_launcher != null) _launcher.SetMinRange(CurrentMinRange());
    }

    void ApplyMaxRange()
    {
        if (_launcher != null) _launcher.SetMaxRange(CurrentMaxRange());
    }

    // ── Create launcher ──

    void CreateLauncher()
    {
        var go = new GameObject("MissileLauncher");
        _launcher = go.AddComponent<MissileLauncher>();
        _launcher.Init(
            _stopper != null ? (Vector2)_stopper.position : Vector2.zero,
            _stopperRadius
        );
        _launcher.SetFireInterval(CurrentFireInterval());
        _launcher.SetDamage(CurrentDamage());
        _launcher.SetBlastRadius(CurrentBlastRadius());
        _launcher.SetMissileSpeed(CurrentSpeed());
        _launcher.SetHomingStrength(CurrentHoming());
        _launcher.SetMinRange(CurrentMinRange());
        _launcher.SetMaxRange(CurrentMaxRange());

        if (_stopper != null)
            _launcher.SetStopper(_stopper);
    }

    // ── Restore from save ──

    public void RestoreUpgrades(int[] levels, int totalInvestment)
    {
        if (levels == null) return;
        _upgrades.RestoreState(levels, totalInvestment);

        ApplyFireRate();
        ApplyDamage();
        ApplyBlastRadius();
        ApplySpeed();
        ApplyHoming();
        ApplyMinRange();
        ApplyMaxRange();
    }

    // ── Cleanup ──

    void OnDestroy()
    {
        if (_launcher != null)
            Destroy(_launcher.gameObject);
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
            SlotFireRate => new UpgradeSlotInfo
            {
                Name = "Fire Rate",
                Description = CurrentFireInterval().ToString("F1") + "s reload",
                Icon = GameField.ClockSprite(64),
                IconColor = new Color(1f, 0.6f, 0.2f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            SlotDamage => new UpgradeSlotInfo
            {
                Name = "Damage",
                Description = "DMG: " + CurrentDamage().ToString("F1"),
                Icon = GameField.BoltSprite(64),
                IconColor = new Color(1f, 0.3f, 0.3f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            SlotBlastRadius => new UpgradeSlotInfo
            {
                Name = "Blast",
                Description = CurrentBlastRadius().ToString("F2") + " radius",
                Icon = GameField.CircleSprite(64),
                IconColor = new Color(1f, 0.8f, 0.2f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            SlotSpeed => new UpgradeSlotInfo
            {
                Name = "Speed",
                Description = CurrentSpeed().ToString("F1") + " u/s",
                Icon = GameField.WaveSprite(64),
                IconColor = new Color(0.4f, 0.8f, 1f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            SlotHoming => new UpgradeSlotInfo
            {
                Name = "Homing",
                Description = lvl == 0 ? "None" : "Strength: " + CurrentHoming().ToString("F1"),
                Icon = GameField.CrosshairSprite(64),
                IconColor = new Color(0.8f, 0.4f, 1f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            SlotMinRange => new UpgradeSlotInfo
            {
                Name = "Min Range",
                Description = CurrentMinRange().ToString("F1") + " units",
                Icon = GameField.CircleSprite(64),
                IconColor = new Color(1f, 0.5f, 0.3f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            SlotMaxRange => new UpgradeSlotInfo
            {
                Name = "Max Range",
                Description = CurrentMaxRange().ToString("F1") + " units",
                Icon = GameField.WallExpandSprite(64),
                IconColor = new Color(0.3f, 0.8f, 0.5f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            _ => default
        };
    }
}
