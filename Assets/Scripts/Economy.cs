using System;
using UnityEngine;

/// <summary>
/// Singleton that tracks player money and purchase counts.
/// Provides exponential pricing and buy methods for stoppers and saws.
/// </summary>
public class Economy : MonoBehaviour
{
    public static Economy Instance { get; private set; }

    [SerializeField] private int startingMoney = 10;
    [SerializeField] private int sawBaseCost = 8;
    [SerializeField] private int stopperBaseCost = 20;
    [SerializeField] private int laserBaseCost = 40;
    [SerializeField] private float costMultiplier = 1.6f;

    private int _money;
    private int _sawsPurchased;
    private int _stoppersPurchased;
    private int _lasersPurchased;

    public event Action<int> OnMoneyChanged;

    public int Money => _money;
    public int SawCost => Cost(sawBaseCost, _sawsPurchased);
    public int StopperCost => Cost(stopperBaseCost, _stoppersPurchased);
    public int LaserCost => Cost(laserBaseCost, _lasersPurchased);
    public int StopperSellPrice => _stoppersPurchased > 0 ? Cost(stopperBaseCost, _stoppersPurchased - 1) : 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _money = startingMoney;
    }

    public void Earn(int amount)
    {
        _money += amount;
        OnMoneyChanged?.Invoke(_money);
    }

    public bool CanAffordSaw() => _money >= SawCost;
    public bool CanAffordStopper() => _money >= StopperCost;
    public bool CanAffordLaser() => _money >= LaserCost;

    public bool TryBuySaw()
    {
        int cost = SawCost;
        if (_money < cost) return false;
        _money -= cost;
        _sawsPurchased++;
        OnMoneyChanged?.Invoke(_money);
        return true;
    }

    public bool TryBuyStopper()
    {
        int cost = StopperCost;
        if (_money < cost) return false;
        _money -= cost;
        _stoppersPurchased++;
        OnMoneyChanged?.Invoke(_money);
        return true;
    }

    public bool TryBuyLaser()
    {
        int cost = LaserCost;
        if (_money < cost) return false;
        _money -= cost;
        _lasersPurchased++;
        OnMoneyChanged?.Invoke(_money);
        return true;
    }

    /// <summary>
    /// Sell price is the total investment (base purchase + all upgrade costs).
    /// </summary>
    public int WeaponSellPrice(Weapon weapon)
    {
        if (weapon == null) return 0;
        return weapon.Upgrades.TotalInvestment;
    }

    public void SellWeapon(Weapon weapon)
    {
        if (weapon == null) return;
        int refund = weapon.Upgrades.TotalInvestment;

        switch (weapon.Type)
        {
            case WeaponType.Saw:
                if (_sawsPurchased <= 0) return;
                _sawsPurchased--;
                break;
            case WeaponType.Laser:
                if (_lasersPurchased <= 0) return;
                _lasersPurchased--;
                break;
            default: return;
        }
        _money += refund;
        OnMoneyChanged?.Invoke(_money);
    }

    public bool TrySellStopper()
    {
        if (_stoppersPurchased <= 0) return false;
        _stoppersPurchased--;
        int refund = Cost(stopperBaseCost, _stoppersPurchased);
        _money += refund;
        OnMoneyChanged?.Invoke(_money);
        return true;
    }

    int Cost(int baseCost, int count)
    {
        return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, count));
    }

#if UNITY_EDITOR
    public void DebugAdjustMoney(int delta)
    {
        _money = Mathf.Max(0, _money + delta);
        OnMoneyChanged?.Invoke(_money);
    }
#endif
}
