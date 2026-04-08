using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns composite pinatas — random connected shapes of coloured squares that
/// fall as one body. Shapes are generated via a center-biased growth algorithm.
/// Oscillates horizontally above the screen. Each square has independent health.
/// Variant selection progresses from 100% Basic to a mix of all five types.
/// </summary>
public class SquareSpawner : MonoBehaviour
{
    [SerializeField] private float spawnInterval   = 1f;
    [SerializeField] private float squareSize      = 0.175f;
    [SerializeField] private float spawnY          = 7f;
    [SerializeField] private float gravityScale    = 0.4f;
    [SerializeField] private float maxHorizSpeed   = 0.5f;
    [SerializeField] private float maxAngularSpeed = 60f;
    [SerializeField] private float maxSpawnAngle   = 35f;
    [SerializeField] private int   gridWidth       = 5;
    [SerializeField] private int   gridHeight      = 5;
    [SerializeField] private int   squareCount     = 0;
    [SerializeField] private float squareHealth    = 1f;
    [SerializeField] private float fieldWidth      = 6f;
    [SerializeField] private float oscillateSpeed  = 1f;

    private float  _timer;
    private Sprite _squareSprite;
    private int    _totalSpawned;

    public float SquareSize => squareSize;
    public void SetGridSize(int w, int h) { gridWidth = w; gridHeight = h; }
    public void SetSquareCount(int count) { squareCount = count; }
    public void SetSpawnInterval(float interval) { spawnInterval = interval; }
    public void SetOscillateSpeed(float speed) { oscillateSpeed = speed; }
    public void SetFieldWidth(float width) { fieldWidth = width; }
    public void SetSquareHealth(float health) { squareHealth = health; }

    void Start()
    {
        _squareSprite = BuildSquareSprite();
        _timer = 9999f; // ensure first pinata spawns immediately
    }

    private static readonly (int, int)[] Dirs = { (1, 0), (-1, 0), (0, 1), (0, -1) };

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < spawnInterval) return;
        _timer = 0f;

        int targetCount = squareCount > 0 ? squareCount : gridWidth * gridHeight;
        var shape = GenerateShape(targetCount);

        // Compute bounding box for oscillation and overlap check
        int minC = int.MaxValue, maxC = int.MinValue;
        int minR = int.MaxValue, maxR = int.MinValue;
        foreach (var (c, r) in shape)
        {
            if (c < minC) minC = c;
            if (c > maxC) maxC = c;
            if (r < minR) minR = r;
            if (r > maxR) maxR = r;
        }
        float pinataWidth  = (maxC - minC + 1) * squareSize;
        float pinataHeight = (maxR - minR + 1) * squareSize;

        float maxX = fieldWidth * 0.5f - pinataWidth * 0.5f;
        if (maxX < 0f) maxX = 0f;
        float spawnX = Mathf.Sin(Time.time * oscillateSpeed) * maxX;

        Vector2 spawnPos = new Vector2(spawnX, spawnY);

        if (Physics2D.OverlapBox(spawnPos, new Vector2(pinataWidth, pinataHeight), 0f) != null)
            return;

        SpawnPinata(spawnX, shape, minC, maxC, minR, maxR);
    }

    void SpawnPinata(float spawnX, List<(int col, int row)> shape,
        int minC, int maxC, int minR, int maxR)
    {
        var variant = ChooseVariant();
        var def = PinataVariantDefs.Get(variant);

        var parent = new GameObject("Pinata");
        parent.transform.position = new Vector3(spawnX, spawnY, 0f);
        parent.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-maxSpawnAngle, maxSpawnAngle));

        var rb = parent.AddComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.mass                   = 2f * def.MassMult;
        rb.gravityScale           = gravityScale * def.GravityMult;
        rb.linearVelocity         = new Vector2(Random.Range(-maxHorizSpeed, maxHorizSpeed), 0f);
        rb.angularVelocity        = Random.Range(-maxAngularSpeed, maxAngularSpeed);

        var pinata = parent.AddComponent<Pinata>();

        // Color: variant-driven or random for Basic
        Color color;
        if (def.Hue < 0f)
            color = Random.ColorHSV(0f, 1f, 0.35f, 0.55f, 0.95f, 1f);
        else
            color = Color.HSVToRGB(def.Hue, def.Saturation, def.Value);

        // Center the shape on the parent transform
        float offsetX = (minC + maxC) * 0.5f;
        float offsetY = (minR + maxR) * 0.5f;

        foreach (var (col, row) in shape)
        {
            var sq = new GameObject("Square");
            sq.transform.SetParent(parent.transform, false);
            sq.transform.localPosition = new Vector3(
                (col - offsetX) * squareSize,
                (row - offsetY) * squareSize,
                0f);
            sq.transform.localScale = Vector3.one * squareSize;

            var sr = sq.AddComponent<SpriteRenderer>();
            sr.sprite       = _squareSprite;
            sr.color        = color;
            sr.sortingOrder = 1;

            sq.AddComponent<BoxCollider2D>();

            var ps = sq.AddComponent<PinataSquare>();
            ps.Init(pinata, squareHealth, col, row, variant);
            pinata.Register(ps);

            // Shield visual for Shielded variant
            if (def.ShieldFraction > 0f)
            {
                var shieldGo = new GameObject("Shield");
                shieldGo.transform.SetParent(sq.transform, false);
                shieldGo.transform.localPosition = Vector3.zero;
                shieldGo.transform.localScale = Vector3.one * 1.15f;

                var shieldSr = shieldGo.AddComponent<SpriteRenderer>();
                shieldSr.sprite = _squareSprite;
                shieldSr.color = new Color(0.3f, 0.5f, 1f, 0.3f);
                shieldSr.sortingOrder = 2;

                ps.SetShieldVisual(shieldSr);
            }
        }

        _totalSpawned++;
    }

    // ── Variant selection ──

    PinataVariantType ChooseVariant()
    {
        // Progression factor: 0 at start, ~1 at 300 spawns
        float t = Mathf.Clamp01(_totalSpawned / 300f);

        // Weights lerp from early (100% basic) to late (diverse mix)
        float basic   = Mathf.Lerp(1.0f, 0.30f, t);
        float armored = Mathf.Lerp(0.0f, 0.20f, t);
        float shield  = Mathf.Lerp(0.0f, 0.15f, Mathf.Clamp01((t - 0.3f) / 0.7f)); // unlocks later
        float swift   = Mathf.Lerp(0.0f, 0.20f, t);
        float heavy   = Mathf.Lerp(0.0f, 0.15f, t);

        float total = basic + armored + shield + swift + heavy;
        float roll = Random.Range(0f, total);

        float cum = basic;
        if (roll < cum) return PinataVariantType.Basic;
        cum += armored;
        if (roll < cum) return PinataVariantType.Armored;
        cum += shield;
        if (roll < cum) return PinataVariantType.Shielded;
        cum += swift;
        if (roll < cum) return PinataVariantType.Swift;
        return PinataVariantType.Heavy;
    }

    /// <summary>
    /// Generates a random connected shape via center-biased growth.
    /// Starts at (0,0) and expands outward, preferring cells closer to center.
    /// Guaranteed to produce a single connected component.
    /// </summary>
    List<(int col, int row)> GenerateShape(int targetCount)
    {
        var filled = new HashSet<(int, int)>();
        var frontierSet = new HashSet<(int, int)>();
        var frontier = new List<(int, int)>();
        var result = new List<(int, int)>(targetCount);

        // Seed at center
        filled.Add((0, 0));
        result.Add((0, 0));

        // Add initial frontier
        foreach (var (dc, dr) in Dirs)
        {
            var n = (dc, dr);
            if (!filled.Contains(n) && frontierSet.Add(n))
                frontier.Add(n);
        }

        while (result.Count < targetCount && frontier.Count > 0)
        {
            // Weighted random pick: prefer cells closer to center
            int picked = WeightedPick(frontier);
            var cell = frontier[picked];

            // Remove picked (swap with last for O(1))
            frontier[picked] = frontier[frontier.Count - 1];
            frontier.RemoveAt(frontier.Count - 1);
            frontierSet.Remove(cell);

            filled.Add(cell);
            result.Add(cell);

            // Add new frontier cells
            foreach (var (dc, dr) in Dirs)
            {
                var n = (cell.Item1 + dc, cell.Item2 + dr);
                if (!filled.Contains(n) && frontierSet.Add(n))
                    frontier.Add(n);
            }
        }

        return result;
    }

    /// <summary>
    /// Picks a random index from the frontier, weighted by proximity to center.
    /// Cells closer to (0,0) are more likely to be picked.
    /// </summary>
    static int WeightedPick(List<(int, int)> frontier)
    {
        float totalWeight = 0f;
        for (int i = 0; i < frontier.Count; i++)
        {
            var (c, r) = frontier[i];
            float dist = Mathf.Sqrt(c * c + r * r);
            totalWeight += 1f / (1f + dist);
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < frontier.Count; i++)
        {
            var (c, r) = frontier[i];
            float dist = Mathf.Sqrt(c * c + r * r);
            cumulative += 1f / (1f + dist);
            if (roll <= cumulative)
                return i;
        }

        return frontier.Count - 1;
    }

    static Sprite BuildSquareSprite()
    {
        const int texSize = 32;
        var tex    = new Texture2D(texSize, texSize);
        var pixels = new Color[texSize * texSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % texSize;
            int y = i / texSize;
            // 1-pixel darker border for outline
            if (x == 0 || x == texSize - 1 || y == 0 || y == texSize - 1)
                pixels[i] = new Color(0.6f, 0.6f, 0.6f, 1f);
            else
                pixels[i] = Color.white;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                             new Vector2(0.5f, 0.5f), texSize);
    }
}
