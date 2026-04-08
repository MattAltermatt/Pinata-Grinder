using UnityEngine;

/// <summary>
/// A single saw blade instance that orbits a stopper.
/// Managed by SawGroup — not a Weapon subclass itself.
/// Uses MovePosition for reliable orbit with physics collisions.
/// </summary>
public class SawBlade : MonoBehaviour
{
    [SerializeField, HideInInspector] private Transform stopper;
    [SerializeField, HideInInspector] private float angle;
    [SerializeField, HideInInspector] private float orbitRadius;
    [SerializeField, HideInInspector] private bool initialized;

    private float _orbitSpeed = 30f;
    private float _selfSpinSpeed = 360f;
    private float _damage = 1f;

    public void Init(Vector2 stopperCenter, float stopperRadius, float bladeRadius, float mass)
    {
        // Center the blade on the stopper edge so it peeks out like a table saw
        orbitRadius = stopperRadius;
        initialized = true;

        transform.position   = (Vector3)(stopperCenter + Vector2.right * orbitRadius);
        transform.localScale = Vector3.one * (bladeRadius * 2f);

        // Render behind the stopper so the stopper covers the inner half
        // sortingOrder 3 ensures it's well behind the stopper (5)

        gameObject.layer = LayerMask.NameToLayer("Weapon");

        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = GameField.SawSprite();
        sr.color        = new Color(0.75f, 0.78f, 0.82f);
        sr.sortingOrder = 3;

        var col = gameObject.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.mass                   = mass;
        rb.gravityScale           = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void SetStopper(Transform stopperTransform)
    {
        stopper = stopperTransform;
    }

    // ── Setters called by SawGroup when upgrades change ──

    public void SetOrbitSpeed(float speed) => _orbitSpeed = speed;

    public void SetBladeRadius(float radius)
    {
        transform.localScale = Vector3.one * (radius * 2f);
    }

    public void SetMass(float mass)
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.mass = mass;
    }

    public void SetDamage(float dmg) => _damage = dmg;

    public void SetAngle(float deg) => angle = deg;

    // ── Physics ──

    void FixedUpdate()
    {
        if (!initialized || stopper == null) return;
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) return;
        angle += _orbitSpeed * Time.fixedDeltaTime;
        float rad = angle * Mathf.Deg2Rad;
        Vector2 center = (Vector2)stopper.position;
        var target = center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;
        rb.MovePosition(target);
    }

    void Update()
    {
        transform.Rotate(0f, 0f, _selfSpinSpeed * Time.deltaTime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.TryGetComponent<PinataSquare>(out var sq))
        {
            sq.TakeDamage(_damage);
            SpawnSparks(collision.GetContact(0).point);
        }
    }

    static void SpawnSparks(Vector2 position)
    {
        var go = new GameObject("SawSparks");
        go.transform.position = position;

        var ps = go.AddComponent<ParticleSystem>();
        var psr = go.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        var main = ps.main;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.9f, 0.4f),
            new Color(1f, 0.6f, 0.1f));
        main.gravityModifier = 0.5f;
        main.maxParticles = 8;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        ps.Play();
        Destroy(go, 1f);
    }
}
