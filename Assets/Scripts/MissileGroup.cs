using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Weapon group that manages multiple MissileLauncher instances on a single stopper.
/// Implements 6 upgrade slots: Fire Rate, Damage, Blast Radius, Missile Speed,
/// Extra Launchers, Homing. Each launcher targets and fires independently.
/// </summary>
public class MissileGroup : Weapon
{
    private const int SlotFireRate    = 0;
    private const int SlotDamage      = 1;
    private const int SlotBlastRadius = 2;
    private const int SlotSpeed       = 3;
    private const int SlotLaunchers   = 4;
    private const int SlotHoming      = 5;
    private const int TotalSlots      = 6;

    private static readonly int[] BaseCosts = { 8, 10, 8, 5, 25, 12 };
    private static readonly int[] MaxLevels = { 20, 0, 20, 20, 19, 10 };

    private const float StartFireRate    = 5f;
    private const float MinFireRate      = 0.5f;
    private const float StartDamage      = 5f;
    private const float DamagePerLevel   = 2.5f;
    private const float StartBlastRadius = 0.4f;
    private const float MaxBlastRadius   = 1.5f;
    private const float StartSpeed       = 1.5f;
    private const float MaxSpeed         = 6f;
    private const float StartHoming      = 0f;
    private const float MaxHoming        = 5f;

    private readonly List<MissileLauncher> _launchers = new();
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
        AddLauncher();
    }

    public void SetStopper(Transform stopperTransform)
    {
        _stopper = stopperTransform;
        foreach (var l in _launchers)
            l.SetStopper(stopperTransform);

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
            case SlotLaunchers:   AddLauncher(); break;
            case SlotHoming:      ApplyHoming(); break;
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

    // ── Apply to all launchers ──

    void ApplyFireRate()
    {
        float interval = CurrentFireInterval();
        foreach (var l in _launchers) l.SetFireInterval(interval);
    }

    void ApplyDamage()
    {
        float d = CurrentDamage();
        foreach (var l in _launchers) l.SetDamage(d);
    }

    void ApplyBlastRadius()
    {
        float r = CurrentBlastRadius();
        foreach (var l in _launchers) l.SetBlastRadius(r);
    }

    void ApplySpeed()
    {
        float s = CurrentSpeed();
        foreach (var l in _launchers) l.SetMissileSpeed(s);
    }

    void ApplyHoming()
    {
        float h = CurrentHoming();
        foreach (var l in _launchers) l.SetHomingStrength(h);
    }

    // ── Add launcher ──

    void AddLauncher()
    {
        var go = new GameObject("MissileLauncher");
        var launcher = go.AddComponent<MissileLauncher>();
        launcher.Init(
            _stopper != null ? (Vector2)_stopper.position : Vector2.zero,
            _stopperRadius
        );
        launcher.SetFireInterval(CurrentFireInterval());
        launcher.SetDamage(CurrentDamage());
        launcher.SetBlastRadius(CurrentBlastRadius());
        launcher.SetMissileSpeed(CurrentSpeed());
        launcher.SetHomingStrength(CurrentHoming());

        if (_stopper != null)
            launcher.SetStopper(_stopper);

        _launchers.Add(launcher);
    }

    // ── Cleanup ──

    void OnDestroy()
    {
        foreach (var l in _launchers)
        {
            if (l != null)
                Destroy(l.gameObject);
        }
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
            SlotLaunchers => new UpgradeSlotInfo
            {
                Name = "Launchers",
                Description = "Count: " + (lvl + 1) + (maxed ? " (MAX)" : ""),
                Icon = GameField.MissileLauncherSprite(64),
                IconColor = Color.white,
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
            _ => default
        };
    }
}
