using UnityEngine;

/// <summary>
/// Fire-and-forget missile projectile. Spawned by MissileLauncher.
/// Moves via transform, optionally homes toward target, detonates on
/// proximity to any alive PinataSquare, dealing AOE damage in a blast radius.
/// </summary>
public class Missile : MonoBehaviour
{
    private Vector2 _direction;
    private float _speed;
    private float _damage;
    private float _blastRadius;
    private float _homingStrength;
    private Pinata _target;

    private float _lifetime;
    private const float MaxLifetime = 10f;

    private ParticleSystem _trail;
    private SpriteRenderer _sr;

    public void Init(Vector2 direction, float speed, float damage,
        float blastRadius, float homingStrength, Pinata target)
    {
        _direction = direction.normalized;
        _speed = speed;
        _damage = damage;
        _blastRadius = blastRadius;
        _homingStrength = homingStrength;
        _target = target;

        gameObject.layer = LayerMask.NameToLayer("Weapon");

        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sprite = GameField.MissileSprite();
        _sr.color = new Color(0.6f, 0.6f, 0.55f);
        _sr.sortingOrder = 3;
        transform.localScale = Vector3.one * 0.15f;

        _trail = CreateTrailSystem();
    }

    ParticleSystem CreateTrailSystem()
    {
        var go = new GameObject("Trail");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(-0.5f, 0f, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        var psr = go.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.6f, 0.1f),
            new Color(0.7f, 0.7f, 0.7f));
        main.gravityModifier = 0f;
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 20;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.01f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, 1.4f);

        ps.Play();
        return ps;
    }

    void Update()
    {
        _lifetime += Time.deltaTime;
        if (_lifetime >= MaxLifetime)
        {
            Detonate();
            return;
        }

        // Homing: curve toward Pinata center of mass
        if (_homingStrength > 0f && _target != null && _target.AliveCount > 0)
        {
            Vector2 center = _target.CenterOfMass();
            Vector2 toTarget = (center - (Vector2)transform.position).normalized;
            _direction = Vector2.Lerp(_direction, toTarget, _homingStrength * Time.deltaTime).normalized;
        }

        // Move
        transform.position += (Vector3)(_direction * _speed * Time.deltaTime);

        // Rotate sprite to face movement direction
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Wall detonation: explode when hitting field boundaries
        if (GameField.Instance != null)
        {
            float halfW = GameField.Instance.FieldWidth * 0.5f;
            float bottomY = -GameField.Instance.CameraHalfHeight;
            Vector2 pos = transform.position;
            if (Mathf.Abs(pos.x) >= halfW || pos.y <= bottomY)
            {
                Detonate();
                return;
            }
        }

        // Contact detonation: explode when overlapping any alive square
        if (CheckContactWithSquare())
        {
            Detonate();
            return;
        }

        // Self-destruct if far off screen
        Vector2 mPos = transform.position;
        if (Mathf.Abs(mPos.x) > 20f || Mathf.Abs(mPos.y) > 20f)
            Destroy(gameObject);
    }

    /// <summary>
    /// Returns true if the missile overlaps any alive PinataSquare's bounds.
    /// Uses axis-aligned box overlap (square half-size) for reliable contact detection.
    /// </summary>
    bool CheckContactWithSquare()
    {
        var squares = PinataSquare.All;
        Vector2 pos = transform.position;

        for (int i = 0; i < squares.Count; i++)
        {
            if (squares[i].IsDead) continue;
            Vector2 sqPos = squares[i].transform.position;
            // Check if missile center is within the square's bounds (AABB overlap)
            float halfSize = squares[i].transform.localScale.x * 0.5f;
            if (Mathf.Abs(pos.x - sqPos.x) <= halfSize && Mathf.Abs(pos.y - sqPos.y) <= halfSize)
                return true;
        }
        return false;
    }

    void Detonate()
    {
        Vector2 pos = transform.position;

        // AOE damage to all alive squares within blast radius
        var squares = PinataSquare.All;
        float r2 = _blastRadius * _blastRadius;
        for (int i = 0; i < squares.Count; i++)
        {
            if (squares[i].IsDead) continue;
            if (((Vector2)squares[i].transform.position - pos).sqrMagnitude <= r2)
                squares[i].TakeDamage(_damage);
        }

        SpawnExplosion(pos);

        // Detach trail so particles can fade naturally
        if (_trail != null)
        {
            _trail.transform.SetParent(null);
            _trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(_trail.gameObject, 1f);
        }

        Destroy(gameObject);
    }

    static void SpawnExplosion(Vector2 position)
    {
        var go = new GameObject("MissileExplosion");
        go.transform.position = position;

        var ps = go.AddComponent<ParticleSystem>();
        var psr = go.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        var main = ps.main;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.1f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.7f, 0.1f),
            new Color(1f, 0.3f, 0.1f));
        main.gravityModifier = 0.3f;
        main.maxParticles = 15;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.07f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        ps.Play();
        Destroy(go, 2f);
    }
}
