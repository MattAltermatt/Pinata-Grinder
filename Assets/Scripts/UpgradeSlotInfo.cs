using UnityEngine;

/// <summary>
/// UI-facing info for one upgrade slot on a weapon.
/// Returned by Weapon.GetSlotInfo() for the radial menu to display.
/// </summary>
public struct UpgradeSlotInfo
{
    public string Name;
    public string Description;
    public Sprite Icon;
    public Color IconColor;
    public int Cost;
    public int Level;
    public int MaxLevel;  // 0 = unlimited
    public bool IsMaxed;
}
