using UnityEngine;

/// <summary>
/// Component placed on stopper colliders.
/// Stoppers block pinatas physically; weapons mounted on them deal damage.
/// Clicking a stopper (via Draggable.OnClicked) opens the weapon shop menu.
/// </summary>
public class Stopper : MonoBehaviour
{
    public Weapon Weapon { get; set; }
    public bool HasWeapon => Weapon != null;

    void Start()
    {
        var drag = GetComponent<Draggable>();
        if (drag != null)
            drag.OnClicked += HandleClick;
    }

    void OnDestroy()
    {
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
