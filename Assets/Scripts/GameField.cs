using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configures the camera and creates the static play field:
/// invisible side walls and the red death line at the bottom.
/// Walls can be rebuilt at runtime via RebuildWalls() for upgrades.
/// </summary>
public class GameField : MonoBehaviour
{
    public static GameField Instance { get; private set; }

    [SerializeField] private float fieldWidth      = 6f;
    [SerializeField] private float cameraHalfHeight = 5f;
    [SerializeField] private float stopperRadius = 0.25f;

    public float FieldWidth => fieldWidth;
    public float CameraHalfHeight => cameraHalfHeight;
    public float StopperRadius => stopperRadius;

    // Wall and line references for rebuilding
    private GameObject _wallLeft, _wallLeftVis;
    private GameObject _wallRight, _wallRightVis;
    private GameObject _wallBottom, _wallBottomVis;
    private GameObject _redLine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        int weaponLayer  = LayerMask.NameToLayer("Weapon");
        int wallLayer    = LayerMask.NameToLayer("Wall");
        int stopperLayer = LayerMask.NameToLayer("Stopper");

        Physics2D.IgnoreLayerCollision(wallLayer, weaponLayer, true);      // blades pass through walls
        Physics2D.IgnoreLayerCollision(weaponLayer, weaponLayer, true);    // blades pass through other weapons
        Physics2D.IgnoreLayerCollision(stopperLayer, weaponLayer, true);   // blades pass through all stoppers

        SetupCamera(cameraHalfHeight);
        BuildWalls(fieldWidth);

        // Initialize economy and stopper factory
        gameObject.AddComponent<Economy>();
        StopperFactory.Init(fieldWidth, cameraHalfHeight, stopperRadius);

        // Start with a single stopper in the center (no saw — player buys it)
        StopperFactory.Instance.SpawnStopper(new Vector2(0f, 1f));

        // UI (must come after Economy is initialized)
        gameObject.AddComponent<EconomyUI>();
        gameObject.AddComponent<StopperMenu>();

        // Global upgrades (applies initial level-0 values, rebuilds walls to starting size)
        gameObject.AddComponent<GlobalUpgrades>();
        gameObject.AddComponent<GlobalUpgradesUI>();

#if UNITY_EDITOR
        gameObject.AddComponent<DebugMode>();
#endif
    }

    static void SetupCamera(float halfHeight)
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic     = true;
        cam.orthographicSize = halfHeight;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.08f, 0.08f, 0.1f);
    }

    /// <summary>
    /// Destroys existing walls and rebuilds them at the new field width.
    /// Called by GlobalUpgrades when the wall size upgrade is purchased.
    /// </summary>
    public void RebuildWalls(float newFieldWidth)
    {
        DestroyWalls();
        fieldWidth = newFieldWidth;
        BuildWalls(newFieldWidth);
    }

    void BuildWalls(float width)
    {
        float wallThickness = cameraHalfHeight * 2f / Screen.height * 2f;

        // Side walls
        CreateVisibleWall("WallLeft",
            new Vector2(-width * 0.5f - 0.5f, 0f), new Vector2(1f, 40f),
            new Vector2(-width * 0.5f, 0f), new Vector3(wallThickness, cameraHalfHeight * 2f, 1f),
            out _wallLeft, out _wallLeftVis);
        CreateVisibleWall("WallRight",
            new Vector2(width * 0.5f + 0.5f, 0f), new Vector2(1f, 40f),
            new Vector2(width * 0.5f, 0f), new Vector3(wallThickness, cameraHalfHeight * 2f, 1f),
            out _wallRight, out _wallRightVis);

        // Bottom wall
        float bottomWallY = -cameraHalfHeight;
        CreateVisibleWall("WallBottom",
            new Vector2(0f, bottomWallY - 0.5f), new Vector2(width + 2f, 1f),
            new Vector2(0f, bottomWallY), new Vector3(width, wallThickness, 1f),
            out _wallBottom, out _wallBottomVis);

        // Death line sits just above the bottom wall
        _redLine = BuildRedLine(bottomWallY + wallThickness, width, wallThickness);
    }

    void DestroyWalls()
    {
        if (_wallLeft != null) Destroy(_wallLeft);
        if (_wallLeftVis != null) Destroy(_wallLeftVis);
        if (_wallRight != null) Destroy(_wallRight);
        if (_wallRightVis != null) Destroy(_wallRightVis);
        if (_wallBottom != null) Destroy(_wallBottom);
        if (_wallBottomVis != null) Destroy(_wallBottomVis);
        if (_redLine != null) Destroy(_redLine);
    }

    void CreateVisibleWall(string wallName, Vector2 colliderPos, Vector2 colliderSize,
                            Vector2 visualPos, Vector3 visualScale,
                            out GameObject colliderGo, out GameObject visualGo)
    {
        colliderGo = new GameObject(wallName);
        colliderGo.transform.position = colliderPos;
        colliderGo.layer = LayerMask.NameToLayer("Wall");
        colliderGo.AddComponent<BoxCollider2D>().size = colliderSize;

        visualGo = new GameObject(wallName + "Visual");
        visualGo.transform.position   = (Vector3)visualPos;
        visualGo.transform.localScale = visualScale;
        var sr = visualGo.AddComponent<SpriteRenderer>();
        sr.sprite       = WhiteSprite();
        sr.color        = Color.white;
        sr.sortingOrder = 10;
    }

    GameObject BuildRedLine(float y, float width, float thickness)
    {
        // Make the death line visually prominent (at least 0.08 units tall)
        float visualThickness = Mathf.Max(thickness, 0.08f);

        var go = new GameObject("RedLine");
        go.transform.position   = new Vector3(0f, y, 0f);
        go.transform.localScale = new Vector3(width, visualThickness, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = WhiteSprite();
        sr.color        = Color.red;
        sr.sortingOrder = 10;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = Vector2.one;

        go.AddComponent<DeathLine>();

        // Soft glow behind the line for visibility
        var glow = new GameObject("RedLineGlow");
        glow.transform.SetParent(go.transform, false);
        glow.transform.localScale = new Vector3(1f, 3f, 1f);
        var glowSr = glow.AddComponent<SpriteRenderer>();
        glowSr.sprite       = WhiteSprite();
        glowSr.color        = new Color(1f, 0f, 0f, 0.25f);
        glowSr.sortingOrder = 9;

        return go;
    }

    // ── Procedural sprite generators (cached) ──

    private static readonly Dictionary<string, Sprite> _spriteCache = new();

    static Sprite GetCached(string key, System.Func<Sprite> factory)
    {
        if (_spriteCache.TryGetValue(key, out var cached) && cached != null)
            return cached;
        var sprite = factory();
        _spriteCache[key] = sprite;
        return sprite;
    }

    public static Sprite CircleSprite(int res = 128)
    {
        return GetCached($"Circle{res}", () => CircleSpriteGen(res));
    }
    static Sprite CircleSpriteGen(int res)
    {
        var tex    = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[res * res];
        float center = res * 0.5f;
        float r2     = (center - 1f) * (center - 1f);
        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dx = x - center, dy = y - center;
            pixels[y * res + x] = (dx * dx + dy * dy <= r2) ? Color.white : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    public static Sprite SawSprite(int res = 128, int teeth = 10)
    {
        return GetCached($"Saw{res}_{teeth}", () => SawSpriteGen(res, teeth));
    }
    static Sprite SawSpriteGen(int res, int teeth)
    {
        var tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[res * res];
        float center = res * 0.5f;
        float outerR = center - 1f;
        float innerR = outerR * 0.6f;
        float toothAngle = 2f * Mathf.PI / teeth;

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dx = x - center, dy = y - center;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);

            if (dist <= innerR)
            {
                pixels[y * res + x] = Color.white;
            }
            else if (dist <= outerR)
            {
                float angle = Mathf.Atan2(dy, dx);
                if (angle < 0f) angle += 2f * Mathf.PI;
                float toothPos = (angle % toothAngle) / toothAngle;
                float t = (dist - innerR) / (outerR - innerR);
                float halfWidth = 0.5f * (1f - t);
                bool inTooth = toothPos >= (0.5f - halfWidth) && toothPos <= (0.5f + halfWidth);
                pixels[y * res + x] = inTooth ? Color.white : Color.clear;
            }
            else
            {
                pixels[y * res + x] = Color.clear;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    public static Sprite DishSprite(int res = 128)
    {
        return GetCached($"Dish{res}", () => DishSpriteGen(res));
    }
    static Sprite DishSpriteGen(int res)
    {
        var tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[res * res];
        float cx = res * 0.5f;
        float cy = res * 0.5f;
        float outerR = cx - 1f;

        Color baseColor = new Color(0.35f, 0.35f, 0.4f);
        Color dishColor = new Color(0.55f, 0.58f, 0.65f);
        Color rimColor  = new Color(0.7f, 0.72f, 0.78f);
        Color emitter   = new Color(0.9f, 0.15f, 0.1f);

        float baseR = outerR * 0.35f;
        float dishDepth = outerR * 0.3f;
        float dishStartX = cx + baseR * 0.5f;

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dx = x - cx;
            float dy = y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            Color c = Color.clear;

            if (dist <= baseR)
                c = baseColor;

            if (dist <= outerR && dx > 0f)
            {
                float normDy = dy / outerR;
                float dishFront = cx + outerR;
                float dishBack = dishFront - dishDepth * (1f - normDy * normDy * 3f);
                float verticalExtent = outerR * 0.85f;
                if (Mathf.Abs(dy) <= verticalExtent * (1f - (dx / outerR) * 0.3f))
                {
                    if (x >= dishBack && x <= dishFront && dist <= outerR)
                    {
                        float rimDist = dishFront - x;
                        if (rimDist < outerR * 0.06f)
                            c = rimColor;
                        else
                            c = dishColor;
                    }
                }
            }

            if (dx > baseR * 0.3f && dx < outerR * 0.7f && Mathf.Abs(dy) < outerR * 0.05f)
                c = baseColor;

            float emitX = cx + outerR * 0.55f;
            float edx = x - emitX;
            float edy = y - cy;
            float emitR = outerR * 0.1f;
            if (edx * edx + edy * edy <= emitR * emitR)
                c = emitter;

            pixels[y * res + x] = c;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    public static Sprite WhiteSprite()
    {
        return GetCached("White", WhiteSpriteGen);
    }
    static Sprite WhiteSpriteGen()
    {
        var tex    = new Texture2D(4, 4);
        var pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    // ── Upgrade icon sprites ──

    /// <summary>
    /// Two horizontal arrows pointing outward (← →) representing wall expansion.
    /// </summary>
    public static Sprite WallExpandSprite(int res = 64)
    {
        return GetCached($"WallExpand{res}", () => WallExpandSpriteGen(res));
    }
    static Sprite WallExpandSpriteGen(int res)
    {
        var tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[res * res];
        float cx = res * 0.5f;
        float cy = res * 0.5f;
        float barHalf = res * 0.35f;
        float barThick = res * 0.06f;
        float headSize = res * 0.18f;

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dx = x - cx, dy = y - cy;
            bool filled = false;

            // Horizontal bar
            if (Mathf.Abs(dx) <= barHalf && Mathf.Abs(dy) <= barThick)
                filled = true;

            // Right arrowhead: triangle at x = cx + barHalf pointing right
            float rx = dx - barHalf;
            if (rx >= 0f && rx <= headSize && Mathf.Abs(dy) <= headSize * (1f - rx / headSize))
                filled = true;

            // Left arrowhead: triangle at x = cx - barHalf pointing left
            float lx = -dx - barHalf;
            if (lx >= 0f && lx <= headSize && Mathf.Abs(dy) <= headSize * (1f - lx / headSize))
                filled = true;

            pixels[y * res + x] = filled ? Color.white : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    /// <summary>
    /// 3x3 grid of small squares representing pinata grid growth.
    /// </summary>
    public static Sprite GridSprite(int res = 64)
    {
        return GetCached($"Grid{res}", () => GridSpriteGen(res));
    }
    static Sprite GridSpriteGen(int res)
    {
        var tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[res * res];
        float margin = res * 0.12f;
        float usable = res - margin * 2f;
        float cellSize = usable / 3f;
        float gap = cellSize * 0.15f;

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float lx = x - margin;
            float ly = y - margin;
            bool filled = false;

            if (lx >= 0 && lx < usable && ly >= 0 && ly < usable)
            {
                float cx2 = lx % cellSize;
                float cy2 = ly % cellSize;
                if (cx2 > gap && cx2 < cellSize - gap && cy2 > gap && cy2 < cellSize - gap)
                    filled = true;
            }

            pixels[y * res + x] = filled ? Color.white : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    /// <summary>
    /// Clock face with two hands representing spawn rate.
    /// </summary>
    public static Sprite ClockSprite(int res = 64)
    {
        return GetCached($"Clock{res}", () => ClockSpriteGen(res));
    }
    static Sprite ClockSpriteGen(int res)
    {
        var tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[res * res];
        float cx = res * 0.5f;
        float cy = res * 0.5f;
        float outerR = cx - 2f;
        float innerR = outerR * 0.85f;
        float handThick = res * 0.04f;

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dx = x - cx, dy = y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            bool filled = false;

            // Circle ring
            if (dist >= innerR && dist <= outerR)
                filled = true;

            // Center dot
            if (dist <= res * 0.06f)
                filled = true;

            // Hour hand (short, pointing to 2 o'clock: ~60 degrees from top)
            float hAngle = 60f * Mathf.Deg2Rad;
            float hLen = outerR * 0.45f;
            float hx = Mathf.Sin(hAngle), hy = Mathf.Cos(hAngle);
            float projH = dx * hx + dy * hy;
            float perpH = Mathf.Abs(-dx * hy + dy * hx);
            if (projH > 0f && projH < hLen && perpH < handThick)
                filled = true;

            // Minute hand (long, pointing to 12 o'clock: straight up)
            float mLen = outerR * 0.75f;
            if (dy > 0f && dy < mLen && Mathf.Abs(dx) < handThick)
                filled = true;

            pixels[y * res + x] = filled ? Color.white : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    /// <summary>
    /// Heart shape representing pinata health.
    /// </summary>
    public static Sprite HeartSprite(int res = 64)
    {
        return GetCached($"Heart{res}", () => HeartSpriteGen(res));
    }
    static Sprite HeartSpriteGen(int res)
    {
        var tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[res * res];
        float cx = res * 0.5f;
        float cy = res * 0.45f;
        float scale = res * 0.012f;

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            // Heart equation: (x^2 + y^2 - 1)^3 - x^2*y^3 <= 0
            float nx = (x - cx) * scale;
            float ny = (y - cy) * scale;
            ny = -ny; // flip so heart points up
            float a = nx * nx + ny * ny - 1f;
            float v = a * a * a - nx * nx * ny * ny * ny;
            pixels[y * res + x] = v <= 0f ? Color.white : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    /// <summary>
    /// Downward bolt/lightning shape representing death line damage.
    /// </summary>
    public static Sprite BoltSprite(int res = 64)
    {
        return GetCached($"Bolt{res}", () => BoltSpriteGen(res));
    }
    static Sprite BoltSpriteGen(int res)
    {
        var tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[res * res];
        float cx = res * 0.5f;

        // Define a zigzag bolt as line segments, then fill pixels near them
        float thickness = res * 0.09f;
        // Bolt points (normalized 0-1, then scaled to res)
        Vector2[] pts = {
            new(0.45f, 0.9f),
            new(0.55f, 0.55f),
            new(0.4f,  0.55f),
            new(0.55f, 0.1f)
        };

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float minDist = float.MaxValue;
            for (int i = 0; i < pts.Length - 1; i++)
            {
                var a = pts[i] * res;
                var b = pts[i + 1] * res;
                float d = DistToSegment(new Vector2(x, y), a, b);
                if (d < minDist) minDist = d;
            }
            pixels[y * res + x] = minDist <= thickness ? Color.white : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        var proj = a + ab * t;
        return Vector2.Distance(p, proj);
    }

    /// <summary>
    /// Missile launcher tube/pod shape facing right. Dark gray cylindrical body
    /// with darker muzzle opening on the right and a mounting base on the left.
    /// </summary>
    public static Sprite MissileLauncherSprite(int res = 128)
    {
        return GetCached($"MissileLauncher{res}", () => MissileLauncherSpriteGen(res));
    }
    static Sprite MissileLauncherSpriteGen(int res)
    {
        var tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[res * res];
        float cx = res * 0.5f;
        float cy = res * 0.5f;

        Color bodyColor  = new Color(0.3f, 0.35f, 0.3f);
        Color muzzle     = new Color(0.2f, 0.22f, 0.2f);
        Color rimColor   = new Color(0.45f, 0.5f, 0.45f);
        Color baseColor  = new Color(0.35f, 0.35f, 0.4f);

        float bodyHalfW  = res * 0.35f;
        float bodyHalfH  = res * 0.18f;
        float bodyLeft   = cx - bodyHalfW * 0.6f;
        float bodyRight  = cx + bodyHalfW;
        float baseR      = res * 0.15f;
        float muzzleR    = bodyHalfH * 0.7f;
        float capR       = bodyHalfH;

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dx = x - cx, dy = y - cy;
            Color c = Color.clear;

            // Mounting base circle on the left
            float bdx = x - (cx - bodyHalfW * 0.4f);
            float bdy = y - cy;
            if (bdx * bdx + bdy * bdy <= baseR * baseR)
                c = baseColor;

            // Main tube body (rounded rectangle)
            if (x >= bodyLeft && x <= bodyRight && Mathf.Abs(dy) <= bodyHalfH)
                c = bodyColor;

            // Rounded cap on the right end
            float capCx = bodyRight;
            float cdx = x - capCx;
            if (cdx > 0 && cdx * cdx + dy * dy <= capR * capR)
                c = bodyColor;

            // Muzzle opening (dark circle at right tip)
            float muzzleCx = bodyRight + capR * 0.3f;
            float mdx = x - muzzleCx;
            float mdy = y - cy;
            if (mdx * mdx + mdy * mdy <= muzzleR * muzzleR)
                c = muzzle;

            // Rim ring around muzzle
            float rimInner = muzzleR * 0.85f;
            float rimOuter = muzzleR * 1.15f;
            float mDist2 = mdx * mdx + mdy * mdy;
            if (mDist2 >= rimInner * rimInner && mDist2 <= rimOuter * rimOuter)
                c = rimColor;

            // Top/bottom tube ridges (subtle highlight lines)
            if (x >= bodyLeft && x <= bodyRight)
            {
                float edgeDist = Mathf.Abs(Mathf.Abs(dy) - bodyHalfH);
                if (edgeDist < res * 0.02f && Mathf.Abs(dy) <= bodyHalfH)
                    c = rimColor;
            }

            pixels[y * res + x] = c;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    /// <summary>
    /// Small elongated missile projectile shape with pointed nose and fins.
    /// White texture intended to be tinted at runtime via SpriteRenderer.color.
    /// </summary>
    public static Sprite MissileSprite(int res = 64)
    {
        return GetCached($"Missile{res}", () => MissileSpriteGen(res));
    }
    static Sprite MissileSpriteGen(int res)
    {
        var tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[res * res];
        float cx = res * 0.5f;
        float cy = res * 0.5f;

        float bodyLen = res * 0.35f;
        float bodyHalfH = res * 0.08f;
        float noseLen = res * 0.15f;
        float finLen = res * 0.12f;
        float finHalfH = res * 0.14f;

        float bodyLeft = cx - bodyLen;
        float bodyRight = cx + bodyLen * 0.3f;
        float noseRight = bodyRight + noseLen;

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dy = y - cy;
            bool filled = false;

            // Main body (rectangle)
            if (x >= bodyLeft && x <= bodyRight && Mathf.Abs(dy) <= bodyHalfH)
                filled = true;

            // Nose cone (triangle tapering to the right)
            if (x > bodyRight && x <= noseRight)
            {
                float t = (x - bodyRight) / noseLen;
                if (Mathf.Abs(dy) <= bodyHalfH * (1f - t))
                    filled = true;
            }

            // Tail fins (two triangles at the back)
            if (x >= bodyLeft - finLen && x <= bodyLeft)
            {
                float t = (bodyLeft - x) / finLen;
                float finTop = bodyHalfH + t * (finHalfH - bodyHalfH);
                if (Mathf.Abs(dy) <= finTop && Mathf.Abs(dy) >= bodyHalfH * (1f - t * 0.5f))
                    filled = true;
                // Also fill the body connection area
                if (Mathf.Abs(dy) <= bodyHalfH)
                    filled = true;
            }

            pixels[y * res + x] = filled ? Color.white : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    /// <summary>
    /// Crosshair/target reticle sprite for homing upgrade icon.
    /// </summary>
    public static Sprite CrosshairSprite(int res = 64)
    {
        return GetCached($"Crosshair{res}", () => CrosshairSpriteGen(res));
    }
    static Sprite CrosshairSpriteGen(int res)
    {
        var tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[res * res];
        float cx = res * 0.5f;
        float cy = res * 0.5f;
        float outerR = cx - 4f;
        float innerR = outerR * 0.7f;
        float lineThick = res * 0.06f;
        float lineLen = outerR;

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dx = x - cx, dy = y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            bool filled = false;

            // Outer ring
            if (dist >= innerR && dist <= outerR)
                filled = true;

            // Crosshair lines (extending from ring outward)
            if (Mathf.Abs(dx) <= lineThick && Mathf.Abs(dy) <= lineLen)
                filled = true;
            if (Mathf.Abs(dy) <= lineThick && Mathf.Abs(dx) <= lineLen)
                filled = true;

            // Center dot
            if (dist <= res * 0.05f)
                filled = true;

            pixels[y * res + x] = filled ? Color.white : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    /// <summary>
    /// Horizontal sine wave representing oscillation speed.
    /// </summary>
    public static Sprite WaveSprite(int res = 64)
    {
        return GetCached($"Wave{res}", () => WaveSpriteGen(res));
    }
    static Sprite WaveSpriteGen(int res)
    {
        var tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[res * res];
        float cx = res * 0.5f;
        float cy = res * 0.5f;
        float amplitude = res * 0.3f;
        float thickness = res * 0.08f;
        float cycles = 2f;

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float nx = (float)x / res;
            float waveY = cy + Mathf.Sin(nx * cycles * 2f * Mathf.PI) * amplitude;
            float dist = Mathf.Abs(y - waveY);

            pixels[y * res + x] = dist <= thickness ? Color.white : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }
}
