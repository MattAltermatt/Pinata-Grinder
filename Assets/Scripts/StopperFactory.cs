using UnityEngine;

/// <summary>
/// Plain C# class that creates stoppers and attaches weapon groups.
/// SawGroup and LaserGroup manage their own child blade/laser instances.
/// </summary>
public class StopperFactory
{
    public static StopperFactory Instance { get; private set; }

    private float _fieldWidth;
    private float _cameraHalfHeight;
    private float _stopperRadius;

    public static void Init(float fieldWidth, float cameraHalfHeight, float stopperRadius)
    {
        Instance = new StopperFactory
        {
            _fieldWidth = fieldWidth,
            _cameraHalfHeight = cameraHalfHeight,
            _stopperRadius = stopperRadius
        };
    }

    public void UpdateFieldWidth(float newFieldWidth)
    {
        _fieldWidth = newFieldWidth;
        var stoppers = Stopper.All;
        float halfW = newFieldWidth * 0.5f;
        foreach (var s in stoppers)
        {
            var drag = s.GetComponent<Draggable>();
            if (drag != null)
                drag.SetBounds(-halfW, halfW, -_cameraHalfHeight, _cameraHalfHeight);

            var pos = s.transform.position;
            pos.x = Mathf.Clamp(pos.x, -halfW, halfW);
            s.transform.position = pos;
        }
    }

    public Vector2 FindClearSpawnPos(Vector2 preferred)
    {
        var stoppers = Stopper.All;
        float minSeparation = _stopperRadius * 4f;

        if (!TooCloseToAny(preferred, stoppers, minSeparation))
            return preferred;

        float halfW = _fieldWidth * 0.5f - _stopperRadius;
        float halfH = _cameraHalfHeight - _stopperRadius;
        float step = _stopperRadius * 2f;

        for (float r = step; r < _fieldWidth; r += step)
        {
            for (float angle = 0f; angle < 360f; angle += 30f)
            {
                float rad = angle * Mathf.Deg2Rad;
                var candidate = preferred + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;
                candidate.x = Mathf.Clamp(candidate.x, -halfW, halfW);
                candidate.y = Mathf.Clamp(candidate.y, -halfH, halfH);

                if (!TooCloseToAny(candidate, stoppers, minSeparation))
                    return candidate;
            }
        }

        return preferred + Vector2.right * step;
    }

    static bool TooCloseToAny(Vector2 pos, System.Collections.Generic.IReadOnlyList<Stopper> stoppers, float minDist)
    {
        float minDist2 = minDist * minDist;
        for (int i = 0; i < stoppers.Count; i++)
        {
            if (((Vector2)stoppers[i].transform.position - pos).sqrMagnitude < minDist2)
                return true;
        }
        return false;
    }

    public Stopper SpawnStopper(Vector2 pos)
    {
        var go = new GameObject("Stopper");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * (_stopperRadius * 2f);
        go.layer = LayerMask.NameToLayer("Stopper");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GameField.CircleSprite();
        sr.color = new Color(0.35f, 0.35f, 0.4f);
        sr.sortingOrder = 5;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        var stopper = go.AddComponent<Stopper>();
        var drag = go.AddComponent<Draggable>();
        drag.SetBounds(-_fieldWidth * 0.5f, _fieldWidth * 0.5f,
                        -_cameraHalfHeight, _cameraHalfHeight);

        return stopper;
    }

    public SawGroup AttachSaw(Stopper stopper)
    {
        var stopperPos = (Vector2)stopper.transform.position;
        var groupGo = new GameObject("SawGroup");
        var group = groupGo.AddComponent<SawGroup>();
        group.Init(stopperPos, _stopperRadius);
        group.SetStopper(stopper.transform);

        // Record initial purchase cost
        group.Upgrades.SetInitialInvestment(Economy.Instance.SawCost);

        stopper.Weapon = group;
        return group;
    }

    public LaserGroup AttachLaser(Stopper stopper)
    {
        var stopperPos = (Vector2)stopper.transform.position;
        var groupGo = new GameObject("LaserGroup");
        var group = groupGo.AddComponent<LaserGroup>();
        group.Init(stopperPos, _stopperRadius);
        group.SetStopper(stopper.transform);

        group.Upgrades.SetInitialInvestment(Economy.Instance.LaserCost);

        stopper.Weapon = group;
        return group;
    }

    public MissileGroup AttachMissile(Stopper stopper)
    {
        var stopperPos = (Vector2)stopper.transform.position;
        var groupGo = new GameObject("MissileGroup");
        var group = groupGo.AddComponent<MissileGroup>();
        group.Init(stopperPos, _stopperRadius);
        group.SetStopper(stopper.transform);

        group.Upgrades.SetInitialInvestment(Economy.Instance.MissileCost);

        stopper.Weapon = group;
        return group;
    }

    public void DestroyStopper(Stopper stopper)
    {
        if (stopper.HasWeapon)
            DetachWeapon(stopper);
        Object.Destroy(stopper.gameObject);
    }

    public void DetachWeapon(Stopper stopper)
    {
        if (stopper.Weapon != null)
        {
            Object.Destroy(stopper.Weapon.gameObject);
            stopper.Weapon = null;

            // Restore stopper sprite visibility
            var sr = stopper.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;
        }
    }
}
