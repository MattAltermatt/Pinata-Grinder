using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Allows a GameObject to be clicked and dragged with the mouse or touch.
/// Requires a Collider2D for raycasting and a Rigidbody2D for MovePosition.
/// Uses the new Input System package.
/// Fires OnClicked if the mouse is released without significant movement.
/// </summary>
public class Draggable : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Camera _cam;
    private bool _dragging;
    private Vector2 _offset;
    private Vector2 _mouseDownWorldPos;
    private float _minX, _maxX, _minY, _maxY;
    private bool _hasBounds;

    private const float ClickThreshold = 0.35f;

    public event Action<GameObject> OnClicked;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _cam = Camera.main;
    }

    public void SetBounds(float minX, float maxX, float minY, float maxY)
    {
        _minX = minX;
        _maxX = maxX;
        _minY = minY;
        _maxY = maxY;
        _hasBounds = true;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mouseWorld = _cam.ScreenToWorldPoint(mouse.position.ReadValue());

        if (mouse.leftButton.wasPressedThisFrame)
        {
            var hit = Physics2D.OverlapPoint(mouseWorld);
            if (hit != null && hit.gameObject == gameObject)
            {
                _dragging = true;
                _offset = (Vector2)transform.position - mouseWorld;
                _mouseDownWorldPos = mouseWorld;
            }
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            if (_dragging)
            {
                float dist = Vector2.Distance(mouseWorld, _mouseDownWorldPos);
                if (dist < ClickThreshold)
                    OnClicked?.Invoke(gameObject);
            }
            _dragging = false;
        }
    }

    void FixedUpdate()
    {
        if (!_dragging) return;
        var mouse = Mouse.current;
        if (mouse == null) return;
        Vector2 mouseWorld = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
        Vector2 target = mouseWorld + _offset;

        if (_hasBounds)
        {
            target.x = Mathf.Clamp(target.x, _minX, _maxX);
            target.y = Mathf.Clamp(target.y, _minY, _maxY);
        }

        _rb.MovePosition(target);
    }
}
