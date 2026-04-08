using UnityEngine;

/// <summary>
/// Tracks per-weapon-instance upgrade levels and total money invested.
/// Each weapon group (SawGroup, LaserGroup) owns one of these.
/// TotalInvestment is refunded on sell.
/// </summary>
public class WeaponUpgradeData
{
    private const float CostMultiplier = 1.6f;

    private readonly int[] _levels;
    private int _totalInvestment;

    public int TotalInvestment => _totalInvestment;

    public WeaponUpgradeData(int slotCount)
    {
        _levels = new int[slotCount];
    }

    public int GetLevel(int slot) => _levels[slot];

    public void SetInitialInvestment(int basePurchaseCost)
    {
        _totalInvestment = basePurchaseCost;
    }

    public int UpgradeCost(int slot, int baseCost)
    {
        return Mathf.RoundToInt(baseCost * Mathf.Pow(CostMultiplier, _levels[slot]));
    }

    public void BuyUpgrade(int slot, int cost)
    {
        _levels[slot]++;
        _totalInvestment += cost;
    }

    public int[] GetLevels()
    {
        return (int[])_levels.Clone();
    }

    public void RestoreState(int[] levels, int totalInvestment)
    {
        for (int i = 0; i < _levels.Length && i < levels.Length; i++)
            _levels[i] = levels[i];
        _totalInvestment = totalInvestment;
    }
}
