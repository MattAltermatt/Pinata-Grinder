using UnityEngine;

public enum WeaponType { None, Saw, Laser, Missile, BlackHole }

/// <summary>
/// Abstract base class for weapon groups that mount on stoppers.
/// SawGroup and LaserGroup extend this. Individual blades/lasers
/// are managed children, not Weapon subclasses themselves.
/// </summary>
public abstract class Weapon : MonoBehaviour
{
    public abstract WeaponType Type { get; }
    public abstract string DisplayName { get; }
    public abstract void Init(Vector2 stopperCenter, float stopperRadius);

    public abstract WeaponUpgradeData Upgrades { get; }
    public abstract int UpgradeSlotCount { get; }
    public abstract UpgradeSlotInfo GetSlotInfo(int slot);
    public abstract bool TryUpgrade(int slot);

    public virtual bool HasDirectionToggle => false;
    public virtual bool IsClockwise => false;
    public virtual void ToggleDirection() { }

    /// <summary>
    /// When true, upgrades are free and ignore max level caps.
    /// Toggled at runtime by DebugMode (spacebar in editor).
    /// </summary>
    public static bool IsDebugMode { get; set; }
}
