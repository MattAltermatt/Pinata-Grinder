using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parent controller for a composite pinata.
/// Holds the Rigidbody2D and manages child square detachment.
/// After a square dies, checks connectivity via flood-fill and splits
/// disconnected groups into separate Pinata objects.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Pinata : MonoBehaviour
{
    private static readonly (int dc, int dr)[] Offsets = { (1, 0), (-1, 0), (0, 1), (0, -1) };

    private Rigidbody2D _rb;
    private readonly List<PinataSquare> _squares = new();
    private readonly Dictionary<(int col, int row), PinataSquare> _grid = new();
    private bool _splitPending;

    public int AliveCount => _squares.Count;

    /// <summary>
    /// World-space center of all remaining alive squares.
    /// </summary>
    public Vector2 CenterOfMass()
    {
        if (_squares.Count == 0) return transform.position;
        Vector2 sum = Vector2.zero;
        int count = 0;
        foreach (var s in _squares)
        {
            if (s == null) continue;
            sum += (Vector2)s.transform.position;
            count++;
        }
        return count > 0 ? sum / count : (Vector2)transform.position;
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Register(PinataSquare sq)
    {
        _squares.Add(sq);
        _grid[(sq.GridCol, sq.GridRow)] = sq;
    }

    public void DetachSquare(PinataSquare sq)
    {
        _squares.Remove(sq);
        _grid.Remove((sq.GridCol, sq.GridRow));

        // Give the dead square its own Rigidbody2D
        var vel = _rb.GetPointVelocity(sq.transform.position);
        var angVel = _rb.angularVelocity;
        var gravScale = _rb.gravityScale;

        sq.transform.SetParent(null);

        var rb = sq.gameObject.AddComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = gravScale;
        rb.linearVelocity = vel;
        rb.angularVelocity = angVel;

        if (_squares.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        // Defer the split check so that AOE kills in the same frame
        // are all processed before we run the connectivity flood-fill.
        _splitPending = true;
    }

    void LateUpdate()
    {
        if (!_splitPending) return;
        _splitPending = false;

        // Purge any squares destroyed between Update and LateUpdate
        _squares.RemoveAll(s => s == null);

        if (_squares.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        // Rebuild grid from current squares (some may have been purged)
        _grid.Clear();
        foreach (var s in _squares)
            _grid[(s.GridCol, s.GridRow)] = s;

        // Check if remaining squares are still connected
        var groups = FindConnectedGroups();
        if (groups.Count <= 1) return;

        // Keep the largest group on this Pinata; split the rest
        groups.Sort((a, b) => b.Count.CompareTo(a.Count));

        var parentAngVel = _rb.angularVelocity;
        var parentGravScale = _rb.gravityScale;

        for (int g = 1; g < groups.Count; g++)
            SpawnSplitPinata(groups[g], parentAngVel, parentGravScale);

        // Rebuild tracking to only contain the kept group
        _squares.Clear();
        _grid.Clear();
        foreach (var s in groups[0])
        {
            _squares.Add(s);
            _grid[(s.GridCol, s.GridRow)] = s;
        }
    }

    private List<List<PinataSquare>> FindConnectedGroups()
    {
        var visited = new HashSet<PinataSquare>();
        var groups = new List<List<PinataSquare>>();

        foreach (var sq in _squares)
        {
            if (!visited.Add(sq)) continue;

            var group = new List<PinataSquare>();
            var queue = new Queue<PinataSquare>();
            queue.Enqueue(sq);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                group.Add(current);

                foreach (var (dc, dr) in Offsets)
                {
                    if (_grid.TryGetValue((current.GridCol + dc, current.GridRow + dr), out var neighbor)
                        && visited.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
            groups.Add(group);
        }
        return groups;
    }

    private void SpawnSplitPinata(List<PinataSquare> group, float angVel, float gravScale)
    {
        // Compute centroid for the new parent position
        var centroid = Vector3.zero;
        foreach (var s in group)
            centroid += s.transform.position;
        centroid /= group.Count;

        var newParent = new GameObject("Pinata");
        newParent.transform.position = centroid;
        newParent.transform.rotation = transform.rotation;

        var newRb = newParent.AddComponent<Rigidbody2D>();
        newRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        newRb.mass = 2f;
        newRb.gravityScale = gravScale;
        newRb.linearVelocity = _rb.GetPointVelocity(centroid);
        newRb.angularVelocity = angVel;

        var newPinata = newParent.AddComponent<Pinata>();

        foreach (var s in group)
        {
            s.transform.SetParent(newParent.transform, true);
            s.ReParent(newPinata);
            newPinata.Register(s);
        }
    }
}
