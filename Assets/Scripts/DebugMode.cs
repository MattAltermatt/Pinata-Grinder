using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
/// <summary>
/// Editor-only debug tools. Entirely stripped from player builds.
/// Space: toggle pause (freezes physics/time).
/// Click (while paused): kill a live pinata square.
/// Up arrow: add $1000.  Down arrow: remove $1000 (min $0).
/// </summary>
public class DebugMode : MonoBehaviour
{
    private bool _paused;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.spaceKey.wasPressedThisFrame)
            TogglePause();

        if (kb.upArrowKey.wasPressedThisFrame)
            Economy.Instance?.DebugAdjustMoney(1000);

        if (kb.downArrowKey.wasPressedThisFrame)
            Economy.Instance?.DebugAdjustMoney(-1000);

        if (_paused && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryKillSquare();
    }

    void TogglePause()
    {
        _paused = !_paused;
        Time.timeScale = _paused ? 0f : 1f;
        Weapon.IsDebugMode = _paused;
        Debug.Log(_paused ? "[Debug] Paused (debug mode ON)" : "[Debug] Resumed (debug mode OFF)");
    }

    void TryKillSquare()
    {
        var cam = Camera.main;
        if (cam == null) return;

        var mousePos = Mouse.current.position.ReadValue();
        var worldPos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0f));

        var hit = Physics2D.OverlapPoint(worldPos);
        if (hit != null && hit.TryGetComponent<PinataSquare>(out var sq) && !sq.IsDead)
            sq.TakeDamage(float.MaxValue);
    }
}
#endif
