using System;
using UnityEngine;

/// <summary>
/// Singleton that tracks player money and purchase counts.
/// Provides exponential pricing and buy methods for stoppers and saws.
/// </summary>
public class Economy : MonoBehaviour
{
    public static Economy Instance { get; private set; }

    [SerializeField] private int startingMoney = 15;
    [SerializeField] private int sawBaseCost = 8;
    [SerializeField] private int stopperBaseCost = 20;
    [SerializeField] private int laserBaseCost = 40;
    [SerializeField] private int missileBaseCost = 60;
    [SerializeField] private int blackHoleBaseCost = 80;
    [SerializeField] private float costMultiplier = 1.6f;

    private int _money;
    private int _sawsPurchased;
    private int _stoppersPurchased;
    private int _lasersPurchased;
    private int _missilesPurchased;
    private int _blackHolesPurchased;

    public event Action<int> OnMoneyChanged;

    public int Money => _money;
    public int SawCost => Cost(sawBaseCost, _sawsPurchased);
    public int StopperCost => Cost(stopperBaseCost, _stoppersPurchased);
    public int LaserCost => Cost(laserBaseCost, _lasersPurchased);
    public int MissileCost => Cost(missileBaseCost, _missilesPurchased);
    public int BlackHoleCost => Cost(blackHoleBaseCost, _blackHolesPurchased);
    public int StopperSellPrice => _stoppersPurchased > 0 ? Cost(stopperBaseCost, _stoppersPurchased - 1) : 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _money = startingMoney;
    }

    public void Earn(int amount)
    {
        _money = Mathf.Max(0, _money + amount);
        OnMoneyChanged?.Invoke(_money);
    }

    public bool CanAffordSaw() => _money >= SawCost;
    public bool CanAffordStopper() => _money >= StopperCost;
    public bool CanAffordLaser() => _money >= LaserCost;
    public bool CanAffordMissile() => _money >= MissileCost;
    public bool CanAffordBlackHole() => _money >= BlackHoleCost;

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

    public bool TryBuyMissile()
    {
        int cost = MissileCost;
        if (_money < cost) return false;
        _money -= cost;
        _missilesPurchased++;
        OnMoneyChanged?.Invoke(_money);
        return true;
    }

    public bool TryBuyBlackHole()
    {
        int cost = BlackHoleCost;
        if (_money < cost) return false;
        _money -= cost;
        _blackHolesPurchased++;
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
            case WeaponType.Missile:
                if (_missilesPurchased <= 0) return;
                _missilesPurchased--;
                break;
            case WeaponType.BlackHole:
                if (_blackHolesPurchased <= 0) return;
                _blackHolesPurchased--;
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

    public void CaptureState(out int money, out int saws, out int stoppers, out int lasers, out int missiles, out int blackHoles)
    {
        money = _money;
        saws = _sawsPurchased;
        stoppers = _stoppersPurchased;
        lasers = _lasersPurchased;
        missiles = _missilesPurchased;
        blackHoles = _blackHolesPurchased;
    }

    public void RestoreState(int money, int saws, int stoppers, int lasers, int missiles, int blackHoles = 0)
    {
        _money = money;
        _sawsPurchased = saws;
        _stoppersPurchased = stoppers;
        _lasersPurchased = lasers;
        _missilesPurchased = missiles;
        _blackHolesPurchased = blackHoles;
        OnMoneyChanged?.Invoke(_money);
    }

#if UNITY_EDITOR
    public void DebugAdjustMoney(int delta)
    {
        _money = Mathf.Max(0, _money + delta);
        OnMoneyChanged?.Invoke(_money);
    }
#endif
}
