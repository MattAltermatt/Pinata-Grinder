using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component placed on stopper colliders.
/// Stoppers block pinatas physically; weapons mounted on them deal damage.
/// Clicking a stopper (via Draggable.OnClicked) opens the weapon shop menu.
/// </summary>
public class Stopper : MonoBehaviour
{
    private static readonly List<Stopper> _allStoppers = new();
    public static IReadOnlyList<Stopper> All => _allStoppers;

    public Weapon Weapon { get; set; }
    public bool HasWeapon => Weapon != null;

    void Awake()
    {
        _allStoppers.Add(this);
    }

    void Start()
    {
        var drag = GetComponent<Draggable>();
        if (drag != null)
            drag.OnClicked += HandleClick;
    }

    void OnDestroy()
    {
        _allStoppers.Remove(this);
        var drag = GetComponent<Draggable>();
        if (drag != null)
            drag.OnClicked -= HandleClick;
    }

    void HandleClick(GameObject go)
    {
        if (StopperMenu.Instance != null)
            StopperMenu.Instance.Show(this);
    }
}
