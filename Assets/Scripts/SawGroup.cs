using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Weapon group that manages multiple SawBlade instances on a single stopper.
/// Implements 5 upgrade slots: Extra Blades, Orbit Speed, Blade Size, Torque, Damage.
/// All blades share upgrade state and orbit equidistantly around the stopper.
/// </summary>
public class SawGroup : Weapon
{
    // Upgrade slot indices
    private const int SlotBlades   = 0;
    private const int SlotSpeed    = 1;
    private const int SlotSize     = 2;
    private const int SlotTorque   = 3;
    private const int SlotDamage   = 4;
    private const int TotalSlots   = 5;

    // Base costs per upgrade type
    private static readonly int[] BaseCosts = { 15, 5, 8, 5, 10 };
    // Max levels (0 = unlimited)
    private static readonly int[] MaxLevels = { 19, 20, 20, 20, 0 };

    // Starting values
    private const float StartSpeed     = 30f;
    private const float MaxSpeed       = 360f;
    private const float StartRadius    = 0.10f;
    private const float MaxRadius      = 0.25f;
    private const float StartMass      = 5f;
    private const float MaxMass        = 100f;
    private const float StartDamage    = 1f;

    private readonly List<SawBlade> _blades = new();
    private WeaponUpgradeData _upgrades;
    private Transform _stopper;
    private float _stopperRadius;

    public override WeaponType Type => WeaponType.Saw;
    public override string DisplayName => "Saw Blade";
    public override WeaponUpgradeData Upgrades => _upgrades;
    public override int UpgradeSlotCount => TotalSlots;

    public override void Init(Vector2 stopperCenter, float stopperRadius)
    {
        _stopperRadius = stopperRadius;
        _upgrades = new WeaponUpgradeData(TotalSlots);
        AddBlade();
    }

    public void SetStopper(Transform stopperTransform)
    {
        _stopper = stopperTransform;
        foreach (var b in _blades)
            b.SetStopper(stopperTransform);
    }

    // ── Upgrade logic ──

    public override bool TryUpgrade(int slot)
    {
        int maxLvl = MaxLevels[slot];
        if (maxLvl > 0 && _upgrades.GetLevel(slot) >= maxLvl) return false;

        int cost = _upgrades.UpgradeCost(slot, BaseCosts[slot]);

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
            case SlotBlades: AddBlade(); break;
            case SlotSpeed:  ApplySpeed(); break;
            case SlotSize:   ApplySize(); break;
            case SlotTorque: ApplyTorque(); break;
            case SlotDamage: ApplyDamage(); break;
        }
    }

    // ── Slot calculations ──

    float CurrentSpeed()
    {
        int lvl = _upgrades.GetLevel(SlotSpeed);
        return StartSpeed + lvl * ((MaxSpeed - StartSpeed) / MaxLevels[SlotSpeed]);
    }

    float CurrentRadius()
    {
        int lvl = _upgrades.GetLevel(SlotSize);
        return StartRadius + lvl * ((MaxRadius - StartRadius) / MaxLevels[SlotSize]);
    }

    float CurrentMass()
    {
        int lvl = _upgrades.GetLevel(SlotTorque);
        return StartMass + lvl * ((MaxMass - StartMass) / MaxLevels[SlotTorque]);
    }

    float CurrentDamage()
    {
        int lvl = _upgrades.GetLevel(SlotDamage);
        return StartDamage + lvl * 0.5f;
    }

    // ── Apply to all blades ──

    void ApplySpeed()
    {
        float spd = CurrentSpeed();
        foreach (var b in _blades) b.SetOrbitSpeed(spd);
    }

    void ApplySize()
    {
        float r = CurrentRadius();
        foreach (var b in _blades) b.SetBladeRadius(r);
    }

    void ApplyTorque()
    {
        float m = CurrentMass();
        foreach (var b in _blades) b.SetMass(m);
    }

    void ApplyDamage()
    {
        float d = CurrentDamage();
        foreach (var b in _blades) b.SetDamage(d);
    }

    // ── Add blade ──

    void AddBlade()
    {
        var go = new GameObject("SawBlade");
        var blade = go.AddComponent<SawBlade>();
        blade.Init(
            _stopper != null ? (Vector2)_stopper.position : Vector2.zero,
            _stopperRadius,
            CurrentRadius(),
            CurrentMass()
        );
        blade.SetOrbitSpeed(CurrentSpeed());
        blade.SetDamage(CurrentDamage());

        if (_stopper != null)
            blade.SetStopper(_stopper);

        _blades.Add(blade);

        // Layer-level ignores (Weapon ↔ Weapon, Weapon ↔ Stopper, Weapon ↔ Wall)
        // handle all collision avoidance — no per-blade IgnoreCollision needed.

        RedistributeAngles();
    }

    void RedistributeAngles()
    {
        int count = _blades.Count;
        for (int i = 0; i < count; i++)
            _blades[i].SetAngle(i * (360f / count));
    }

    // ── Cleanup ──

    void OnDestroy()
    {
        foreach (var b in _blades)
        {
            if (b != null)
                Destroy(b.gameObject);
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
            SlotBlades => new UpgradeSlotInfo
            {
                Name = "Blades",
                Description = "Count: " + (lvl + 1) + (maxed ? " (MAX)" : ""),
                Icon = GameField.SawSprite(64),
                IconColor = new Color(0.75f, 0.78f, 0.82f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            SlotSpeed => new UpgradeSlotInfo
            {
                Name = "Speed",
                Description = CurrentSpeed().ToString("F0") + " deg/s",
                Icon = GameField.WaveSprite(64),
                IconColor = new Color(0.4f, 0.8f, 1f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            SlotSize => new UpgradeSlotInfo
            {
                Name = "Size",
                Description = "Radius: " + CurrentRadius().ToString("F3"),
                Icon = GameField.WallExpandSprite(64),
                IconColor = new Color(0.4f, 1f, 0.4f),
                Cost = cost, Level = lvl, MaxLevel = maxLvl, IsMaxed = maxed
            },
            SlotTorque => new UpgradeSlotInfo
            {
                Name = "Torque",
                Description = "Mass: " + CurrentMass().ToString("F0"),
                Icon = GameField.GridSprite(64),
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
            _ => default
        };
    }
}
