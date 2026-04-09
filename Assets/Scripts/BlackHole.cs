using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A single black hole vortex instance that pulls pinatas inward and damages
/// squares within its core radius. Managed by BlackHoleGroup — not a Weapon
/// subclass itself. Uses Energy damage type.
/// </summary>
public class BlackHole : MonoBehaviour
{
    [SerializeField, HideInInspector] private Transform stopper;
    [SerializeField, HideInInspector] private bool initialized;
    [SerializeField, HideInInspector] private float _stopperRadius;

    private float _pullRange = 0.4f;
    private float _pullForce = 15f;
    private float _coreRadius = 0.3f;
    private float _coreDPS = 1f;
    private int _maxTargets = 1;

    private SpriteRenderer _vortexSr;
    private SpriteRenderer _rangeSr;
    private GameObject _rangeGo;
    private ParticleSystem _swirlParticles;
    private GameObject _swirlGo;
    private float _pulseTimer;

    // Reusable collections to avoid per-frame allocations
    private readonly HashSet<Rigidbody2D> _uniqueBodies = new();
    private readonly List<(Rigidbody2D rb, float dist)> _sortedBodies = new();
    private static readonly System.Comparison<(Rigidbody2D rb, float dist)> DistComparer =
        (a, b) => a.dist.CompareTo(b.dist);

    public void Init(Vector2 stopperCenter, float stopperRadius)
    {
        initialized = true;
        _stopperRadius = stopperRadius;
        transform.position = (Vector3)stopperCenter;
        gameObject.layer = LayerMask.NameToLayer("Weapon");

        _vortexSr = gameObject.AddComponent<SpriteRenderer>();
        _vortexSr.sprite = GameField.CircleSprite();
        _vortexSr.color = new Color(0.15f, 0.05f, 0.25f);
        _vortexSr.sortingOrder = 4;
        transform.localScale = Vector3.one * (stopperRadius * 2f);

        // Range indicator — NOT parented to vortex so it has independent world-space scale
        var rangeGo = new GameObject("RangeIndicator");
        _rangeSr = rangeGo.AddComponent<SpriteRenderer>();
        _rangeSr.sprite = GameField.CircleSprite();
        _rangeSr.color = new Color(0.4f, 0.1f, 0.6f, 0.06f);
        _rangeSr.sortingOrder = 1;
        _rangeGo = rangeGo;
        UpdateRangeVisual();

        _swirlParticles = CreateSwirlSystem();
    }

    ParticleSystem CreateSwirlSystem()
    {
        // Not parented to vortex — uses world space to avoid scale distortion
        var go = new GameObject("SwirlParticles");
        _swirlGo = go;

        var ps = go.AddComponent<ParticleSystem>();
        var psr = go.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.5f, 0.2f, 0.8f),
            new Color(0.2f, 0.1f, 0.5f));
        main.gravityModifier = 0f;
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 15;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = _pullRange * 0.8f;
        shape.radiusThickness = 0f; // Emit only from the edge

        // Orbital velocity to create spiral effect
        // All three orbital axes must use the same curve mode
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.orbitalY = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.orbitalZ = new ParticleSystem.MinMaxCurve(2f, 4f);
        vel.radial = new ParticleSystem.MinMaxCurve(-1f, -0.5f);

        // Shrink over lifetime (particles get smaller as they approach center)
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.2f)));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        return ps;
    }

    public void SetStopper(Transform stopperTransform) => stopper = stopperTransform;

    // ── Setters called by BlackHoleGroup when upgrades change ──

    public void SetPullRange(float range)
    {
        _pullRange = range;
        UpdateRangeVisual();
        UpdateSwirlRadius();
    }

    public void SetPullForce(float force) => _pullForce = force;
    public void SetCoreRadius(float radius) => _coreRadius = radius;
    public void SetCoreDPS(float dps) => _coreDPS = dps;
    public void SetMaxTargets(int count) => _maxTargets = count;

    void UpdateRangeVisual()
    {
        if (_rangeGo == null) return;
        // World-space scale: diameter = pullRange * 2
        _rangeGo.transform.localScale = Vector3.one * (_pullRange * 2f);
    }

    void UpdateSwirlRadius()
    {
        if (_swirlParticles == null) return;
        var shape = _swirlParticles.shape;
        shape.radius = _pullRange * 0.8f;
    }

    void FixedUpdate()
    {
        if (!initialized || stopper == null) return;

        transform.position = stopper.position;
        if (_rangeGo != null) _rangeGo.transform.position = stopper.position;
        if (_swirlGo != null) _swirlGo.transform.position = stopper.position;
        Vector2 center = (Vector2)transform.position;

        // Collect alive squares within pull range
        var squares = PinataSquare.All;
        float range2 = _pullRange * _pullRange;
        // Effective damage radius: at minimum, anything touching the stopper takes damage
        float effectiveCore = Mathf.Max(_coreRadius, _stopperRadius + 0.15f);
        float core2 = effectiveCore * effectiveCore;

        _uniqueBodies.Clear();
        _sortedBodies.Clear();

        for (int i = 0; i < squares.Count; i++)
        {
            var sq = squares[i];
            if (sq.IsDead) continue;

            Vector2 sqPos = (Vector2)sq.transform.position;
            float dist2 = (sqPos - center).sqrMagnitude;

            if (dist2 > range2) continue;

            // Core damage
            if (dist2 <= core2)
                sq.TakeDamage(_coreDPS * Time.fixedDeltaTime, DamageType.Energy);

            var rb = sq.ParentRigidbody;
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic && _uniqueBodies.Add(rb))
            {
                float dist = Mathf.Sqrt(dist2);
                _sortedBodies.Add((rb, dist));
            }
        }

        // Sort by distance and apply pull to up to _maxTargets bodies
        _sortedBodies.Sort(DistComparer);
        int pullCount = Mathf.Min(_maxTargets, _sortedBodies.Count);

        for (int i = 0; i < pullCount; i++)
        {
            var (rb, dist) = _sortedBodies[i];
            Vector2 toCenter = center - (Vector2)rb.transform.position;
            float distClamped = Mathf.Max(dist, 0.1f);
            // Inverse-distance scaling: stronger pull when closer
            float forceMag = _pullForce / distClamped;
            rb.AddForce(toCenter.normalized * forceMag, ForceMode2D.Force);
        }
    }

    void Update()
    {
        if (!initialized) return;

        _pulseTimer += Time.deltaTime * 3f;
        if (_vortexSr != null)
        {
            float alpha = 0.9f + Mathf.Sin(_pulseTimer) * 0.1f;
            var c = _vortexSr.color;
            c.a = alpha;
            _vortexSr.color = c;
        }
    }

    void OnDestroy()
    {
        if (_rangeGo != null) Destroy(_rangeGo);
        if (_swirlGo != null) Destroy(_swirlGo);
    }
}
