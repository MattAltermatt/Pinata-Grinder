using UnityEngine;

/// <summary>
/// A single laser turret instance that locks onto targets and fires a beam.
/// Managed by LaserGroup — not a Weapon subclass itself.
/// Rotates gradually toward targets (speed is upgradeable).
/// Prefers targets closer to current aim direction when acquiring.
/// </summary>
public class Laser : MonoBehaviour
{
    [SerializeField, HideInInspector] private Transform stopper;
    [SerializeField, HideInInspector] private bool initialized;
    [SerializeField, HideInInspector] private float orbitRadius;

    private float _damagePerSecond = 1f;
    private float _cooldownDuration = 2f;
    private float _maxRange = 1f;
    private float _rotationSpeed = 30f;

    private PinataSquare _target;
    private float _cooldownTimer;
    private bool _isCoolingDown;
    private LineRenderer _beamLine;
    private Transform _emitPoint;
    private ParticleSystem _dishSparkle;
    private ParticleSystem _hitSparkle;
    private GameObject _hitSparkleGo;

    public void Init(Vector2 stopperCenter, float stopperRadius)
    {
        orbitRadius = stopperRadius;
        initialized = true;

        transform.position = (Vector3)stopperCenter;
        transform.localScale = Vector3.one * (stopperRadius * 2f);

        gameObject.layer = LayerMask.NameToLayer("Weapon");

        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GameField.DishSprite();
        sr.sortingOrder = 4;

        // Beam
        var beamGo = new GameObject("Beam");
        beamGo.transform.SetParent(transform, false);
        _beamLine = beamGo.AddComponent<LineRenderer>();
        _beamLine.useWorldSpace = true;
        _beamLine.positionCount = 2;
        _beamLine.startWidth = 0.03f;
        _beamLine.endWidth = 0.03f;
        _beamLine.startColor = Color.red;
        _beamLine.endColor = new Color(1f, 0.3f, 0.2f, 0.7f);
        _beamLine.sortingOrder = 3;
        _beamLine.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        _beamLine.enabled = false;

        // Emit point
        var emitGo = new GameObject("EmitPoint");
        emitGo.transform.SetParent(transform, false);
        emitGo.transform.localPosition = new Vector3(0.55f * 0.5f, 0f, 0f);
        _emitPoint = emitGo.transform;

        // Dish sparkle
        _dishSparkle = CreateSparkleSystem("DishSparkle", transform, 6,
            0.04f, 0.1f, 0.08f, Color.red, new Color(1f, 0.5f, 0.3f));
        _dishSparkle.transform.localPosition = new Vector3(0.55f * 0.5f, 0f, 0f);

        // Hit sparkle
        _hitSparkleGo = new GameObject("HitSparkle");
        _hitSparkle = CreateSparkleSystem("HitSparklePS", _hitSparkleGo.transform, 8,
            0.03f, 0.08f, 0.12f, Color.red, new Color(1f, 0.4f, 0.2f));
        _hitSparkleGo.SetActive(false);
    }

    ParticleSystem CreateSparkleSystem(string name, Transform parent, int rate,
        float minSize, float maxSize, float radius, Color color1, Color color2)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var ps = go.AddComponent<ParticleSystem>();
        var psr = go.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(color1, color2);
        main.gravityModifier = 0f;
        main.maxParticles = 20;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    public void SetStopper(Transform stopperTransform) => stopper = stopperTransform;

    // ── Setters called by LaserGroup when upgrades change ──

    public void SetRotationSpeed(float degPerSec) => _rotationSpeed = degPerSec;
    public void SetMaxRange(float range) => _maxRange = range;
    public void SetDamage(float dps) => _damagePerSecond = dps;

    void OnDestroy()
    {
        if (_hitSparkleGo != null)
            Destroy(_hitSparkleGo);
    }

    void Update()
    {
        if (!initialized || stopper == null) return;

        transform.position = stopper.position;

        if (_isCoolingDown)
        {
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer <= 0f)
                _isCoolingDown = false;
            SetFiring(false);
            return;
        }

        if (_target == null)
        {
            AcquireTarget();
        }
        else if (_target.IsDead)
        {
            _target = null;
            _isCoolingDown = true;
            _cooldownTimer = _cooldownDuration;
            SetFiring(false);
            return;
        }

        // Drop target if out of range
        if (_target != null)
        {
            float dist2 = ((Vector2)_target.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (dist2 > _maxRange * _maxRange)
            {
                _target = null;
                SetFiring(false);
            }
        }

        if (_target != null)
        {
            // Gradual rotation toward target
            Vector2 dir = (Vector2)_target.transform.position - (Vector2)transform.position;
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var targetRot = Quaternion.Euler(0f, 0f, targetAngle);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);

            // Only fire if roughly aimed at target (within 15 degrees)
            float aimError = Quaternion.Angle(transform.rotation, targetRot);
            if (aimError <= 15f)
            {
                SetFiring(true);
                _beamLine.SetPosition(0, _emitPoint.position);
                _beamLine.SetPosition(1, _target.transform.position);
                _hitSparkleGo.transform.position = _target.transform.position;
                _target.TakeDamage(_damagePerSecond * Time.deltaTime);
            }
            else
            {
                SetFiring(false);
            }
        }
        else
        {
            SetFiring(false);
        }
    }

    void SetFiring(bool firing)
    {
        _beamLine.enabled = firing;

        if (firing)
        {
            if (!_dishSparkle.isPlaying) _dishSparkle.Play();
            _hitSparkleGo.SetActive(true);
            if (!_hitSparkle.isPlaying) _hitSparkle.Play();
        }
        else
        {
            if (_dishSparkle.isPlaying) _dishSparkle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (_hitSparkle.isPlaying) _hitSparkle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    void AcquireTarget()
    {
        var squares = FindObjectsByType<PinataSquare>();
        float bestScore = float.MaxValue;
        PinataSquare best = null;

        float maxRange2 = _maxRange * _maxRange;
        Vector2 aimDir = transform.right;

        for (int i = 0; i < squares.Length; i++)
        {
            if (squares[i].IsDead) continue;
            Vector2 toTarget = (Vector2)squares[i].transform.position - (Vector2)transform.position;
            float dist2 = toTarget.sqrMagnitude;
            if (dist2 > maxRange2) continue;

            // Score: distance + angular penalty (prefer targets closer to current aim)
            float dist = Mathf.Sqrt(dist2);
            float angleDiff = Vector2.Angle(aimDir, toTarget.normalized);
            float score = dist + angleDiff * 0.01f;

            if (score < bestScore)
            {
                bestScore = score;
                best = squares[i];
            }
        }
        _target = best;
    }
}
