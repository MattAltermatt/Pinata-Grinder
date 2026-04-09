using System;

/// <summary>
/// Root save data container. Serialized to JSON via JsonUtility.
/// All fields must be public for JsonUtility serialization.
/// </summary>
[Serializable]
public class SaveData
{
    public int money;
    public int sawsPurchased;
    public int stoppersPurchased;
    public int lasersPurchased;
    public int missilesPurchased;
    public int blackHolesPurchased;
    public GlobalUpgradesSaveData globalUpgrades;
    public StopperSaveData[] stoppers;
}

[Serializable]
public class GlobalUpgradesSaveData
{
    public int wallLevel;
    public int pinataLevel;
    public int spawnerLevel;
    public int oscillationLevel;
    public int healthLevel;
    public int deathLineDamageLevel;
}

[Serializable]
public class StopperSaveData
{
    public float posX;
    public float posY;
    public int weaponType; // WeaponType enum cast to int (0=None, 1=Saw, 2=Laser, 3=Missile, 4=BlackHole)
    public int[] upgradeLevels;
    public int totalInvestment;
    public int directionMultiplier; // 1 or -1, only used by SawGroup
}
