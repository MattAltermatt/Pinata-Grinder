using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Weapon group that manages multiple Laser instances on a single stopper.
/// Implements 5 upgrade slots: Aim Speed, Range, Damage, Extra Lasers, Cooldown.
/// Each laser targets independently. All share upgrade state.
/// </summary>
public class LaserGroup : Weapon
{
    private const int SlotAimSpeed   = 0;
    private const int SlotRange      = 1;
    private const int SlotDamage     = 2;
    private const int SlotLasers     = 3;
    private const int SlotCooldown   = 4;
    private const int TotalSlots     = 5;

    private static readonly int[] BaseCosts = { 5, 8, 10, 20, 8 };
    private static readonly int[] MaxLevels = { 20, 20, 0, 19, 20 };

    private const float StartAimSpeed = 30f;
    private const float MaxAimSpeed   = 360f;
    private const float StartRange    = 1.5f;
    private const float StartDamage   = 1f;
    private const float StartCooldown = 3f;
    private const float MinCooldown   = 0.1f;

    private readonly List<Laser> _lasers = new();
    private WeaponUpgradeData _upgrades;
    private Transform _stopper;
    private float _stopperRadius;

    // Cost multiplier: non-laser upgrades cost more when you have more lasers
    float InstanceCostMultiplier() => 1f + (_lasers.Count - 1) * 0.5f;

    public override WeaponType Type => WeaponType.Laser;
    public override string DisplayName => "Laser";
    public override WeaponUpgradeData Upgrades => _upgrades;
    public override int UpgradeSlotCount => TotalSlots;

    public override void Init(Vector2 stopperCenter, float stopperRadius)
    {
        _stopperRadius = stopperRadius;
        _upgrades = new WeaponUpgradeData(TotalSlots);
        AddLaser();
    }

    public void SetStopper(Transform stopperTransform)
    {
        _stopper = stopperTransform;
        foreach (var l in _lasers)
            l.SetStopper(stopperTransform);

        // Hide stopper sprite — laser replaces it visually
        var sr = stopperTransform.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
    }

    // ── Upgrade logic ──

    public override bool TryUpgrade(int slot)
    {
        int maxLvl = MaxLevels[slot];
        float mult = slot == SlotLasers ? 1f : InstanceCostMultiplier();
        int cost = _upgrades.UpgradeCost(slot, BaseCosts[slot], mult);

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
            case SlotAimSpeed: ApplyAimSpeed(); break;
            case SlotRange:    ApplyRange(); break;
            case SlotDamage:   ApplyDamage(); break;
            case SlotLasers:   AddLaser(); break;
            case SlotCooldown: ApplyCooldown(); break;
        }
    }

    // ── Slot calculations ──

    float CurrentAimSpeed()
    {
        int lvl = _upgrades.GetLevel(SlotAimSpeed);
        return StartAimSpeed + lvl * ((MaxAimSpeed - StartAimSpeed) / MaxLevels[SlotAimSpeed]);
    }

    float CurrentRange()
    {
        int lvl = _upgrades.GetLevel(SlotRange);
        float maxFieldWidth = GlobalUpgrades.Instance != null
            ? GlobalUpgrades.Instance.MaxFieldWidth()
            : 15f;
        return StartRange + lvl * ((maxFieldWidth - StartRange) / MaxLevels[SlotRange]);
    }

    float CurrentDamage()
    {
        int lvl = _upgrades.GetLevel(SlotDamage);
        return StartDamage + lvl * 0.5f;
    }

    float CurrentCooldown()
    {
        int lvl = _upgrades.GetLevel(SlotCooldown);
        return StartCooldown - lvl * ((StartCooldown - MinCooldown) / MaxLevels[SlotCooldown]);
    }

    // ── Apply to all lasers ──

    void ApplyAimSpeed()
    {
        float spd = CurrentAimSpeed();
        foreach (var l in _lasers) l.SetRotationSpeed(spd);
    }

    void ApplyRange()
    {
        float r = CurrentRange();
        foreach (var l in _lasers) l.SetMaxRange(r);
    }

    void ApplyDamage()
    {
        float d = CurrentDamage();
        foreach (var l in _lasers) l.SetDamage(d);
    }

    void ApplyCooldown()
    {
        float cd = CurrentCooldown();
        foreach (var l in _lasers) l.SetCooldown(cd);
    }

    // ── Add laser ──

    void AddLaser()
    {
        var go = new GameObject("Laser");
        var laser = go.AddComponent<Laser>();
        laser.Init(
            _stopper != null ? (Vector2)_stopper.position : Vector2.zero,
            _stopperRadius
        );
        laser.SetRotationSpeed(CurrentAimSpeed());
        laser.SetMaxRange(CurrentRange());
        laser.SetDamage(CurrentDamage());
        laser.SetCooldown(CurrentCooldown());

        if (_stopper != null)
            laser.SetStopper(_stopper);

        _lasers.Add(laser);
    }

    // ── Restore from save ──

    public void RestoreUpgrades(int[] levels, int totalInvestment)
    {
        if (levels == null) return;
        _upgrades.RestoreState(levels, totalInvestment);

        // Init() already added 1 laser; add extras for the saved lasers level
        for (int i = 0; i < levels[SlotLasers]; i++)
            AddLaser();

        ApplyAimSpeed();
        ApplyRange();
        ApplyDamage();
        ApplyCooldown();
    }

    // ── Cleanup ──

    void OnDestroy()
    {
        foreach (var l in _lasers)
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
        float mult = slot == SlotLasers ? 1f : InstanceCostMultiplier();
        int cost = _upgrades.UpgradeCost(slot, BaseCosts[slot], mult);

        return slot switch
        {
            SlotAimSpeed => new UpgradeSlotInfo
            {
                Name = "Aim",
                Description = CurrentAimSpeed().ToString("F0") + " deg/s",
                Icon = GameField.WaveSprite(64),
                IconColor = new Color(0.4f, 0.8f, 1f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            SlotRange => new UpgradeSlotInfo
            {
                Name = "Range",
                Description = CurrentRange().ToString("F1") + " units",
                Icon = GameField.ClockSprite(64),
                IconColor = new Color(0.4f, 1f, 0.4f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            SlotDamage => new UpgradeSlotInfo
            {
                Name = "Damage",
                Description = "DPS: " + CurrentDamage().ToString("F1"),
                Icon = GameField.BoltSprite(64),
                IconColor = new Color(1f, 0.3f, 0.3f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            SlotLasers => new UpgradeSlotInfo
            {
                Name = "Lasers",
                Description = "Count: " + (lvl + 1) + (maxed ? " (MAX)" : ""),
                Icon = GameField.DishSprite(64),
                IconColor = Color.white,
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            SlotCooldown => new UpgradeSlotInfo
            {
                Name = "Cooldown",
                Description = CurrentCooldown().ToString("F1") + "s",
                Icon = GameField.ClockSprite(64),
                IconColor = new Color(1f, 0.6f, 0.2f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            _ => default
        };
    }
}
