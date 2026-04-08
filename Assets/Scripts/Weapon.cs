using UnityEngine;

public enum WeaponType { None, Saw, Laser }

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
}
