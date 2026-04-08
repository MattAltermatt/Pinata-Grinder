using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behaviour attached to each individual square within a composite pinata.
/// Tracks health, takes damage (with variant resistance), darkens/shrinks and
/// detaches when killed. Dead squares earn money (based on max health and
/// variant reward multiplier) when hitting the death line. Live squares take
/// death line damage; if killed there, earn only $1.
///
/// Death line processing is deferred to LateUpdate to avoid modifying physics
/// state (adding Rigidbody2D via DetachSquare) inside trigger callbacks, which
/// causes stack overflow in WebGL's single-threaded runtime.
/// </summary>
public class PinataSquare : MonoBehaviour
{
    private static readonly List<PinataSquare> _allSquares = new();
    public static IReadOnlyList<PinataSquare> All => _allSquares;

    private SpriteRenderer _sr;
    private Pinata _pinata;
    private float _health;
    private float _maxHealth;
    private bool _isDead;
    private int _gridCol;
    private int _gridRow;
    private bool _onDeathLine;
    private bool _pendingDestroy;

    // Variant fields
    private PinataVariantType _variant;
    private float _rewardMult = 1f;
    private float _shieldHP;
    private PinataVariantDef _variantDef;
    private SpriteRenderer _shieldVisual;

    public bool IsDead => _isDead;
    public int GridCol => _gridCol;
    public int GridRow => _gridRow;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _allSquares.Add(this);
    }

    void OnDestroy()
    {
        _allSquares.Remove(this);
    }

    void Update()
    {
        // Safety cleanup: destroy squares that fall well below the screen
        if (transform.position.y < -8f)
            Destroy(gameObject);
    }

    public void Init(Pinata parent, float health, int col, int row,
        PinataVariantType variant = PinataVariantType.Basic)
    {
        _pinata = parent;
        _health = health;
        _maxHealth = health;
        _gridCol = col;
        _gridRow = row;
        _variant = variant;
        _variantDef = PinataVariantDefs.Get(variant);
        _rewardMult = _variantDef.RewardMult;

        // Shield setup for Shielded variant
        if (_variantDef.ShieldFraction > 0f)
            _shieldHP = health * _variantDef.ShieldFraction;
    }

    public void SetShieldVisual(SpriteRenderer shieldSr)
    {
        _shieldVisual = shieldSr;
    }

    public void ReParent(Pinata newParent)
    {
        _pinata = newParent;
    }

    public void TakeDamage(float amount, DamageType type = DamageType.Physical)
    {
        if (_isDead) return;

        // Apply variant resistance multiplier
        float mult = type switch
        {
            DamageType.Physical => _variantDef.PhysicalMult,
            DamageType.Energy => _variantDef.EnergyMult,
            DamageType.Explosive => _variantDef.ExplosiveMult,
            _ => 1f
        };
        float effective = amount * mult;

        // Shielded variant: Energy damage drains shield first
        if (_shieldHP > 0f && type == DamageType.Energy)
        {
            float absorbed = Mathf.Min(_shieldHP, effective);
            _shieldHP -= absorbed;
            effective -= absorbed;

            if (_shieldHP <= 0f)
                DestroyShieldVisual();

            if (effective <= 0f) return;
        }

        _health -= effective;
        if (_health <= 0f)
            Die();
    }

    void DestroyShieldVisual()
    {
        if (_shieldVisual != null)
        {
            Destroy(_shieldVisual.gameObject);
            _shieldVisual = null;
        }
    }

    void Die()
    {
        _isDead = true;
        _sr.color *= 0.5f;
        transform.localScale *= 0.75f;
        gameObject.layer = LayerMask.NameToLayer("DeadSquare");
        DestroyShieldVisual();
        _pinata.DetachSquare(this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<DeathLine>(out _)) return;
        _onDeathLine = true;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.TryGetComponent<DeathLine>(out _)) return;
        _onDeathLine = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<DeathLine>(out _))
            _onDeathLine = false;
    }

    void LateUpdate()
    {
        if (!_onDeathLine || _pendingDestroy) return;

        // Already-dead squares (weapon kills) — collect with confetti + full value
        if (_isDead)
        {
            _pendingDestroy = true;
            ConfettiBurst.Spawn(transform.position, _sr.color);
            Economy.Instance?.Earn(Mathf.Max(1, Mathf.RoundToInt(_maxHealth * 2f * _rewardMult)));
            Destroy(gameObject);
            return;
        }

        // Live squares take death line DPS
        float dps = GlobalUpgrades.Instance != null
            ? GlobalUpgrades.Instance.DeathLineDamage
            : 1f;
        TakeDamage(dps * Time.deltaTime);

        // If killed by the death line, earn only $1 (no confetti)
        if (_isDead)
        {
            _pendingDestroy = true;
            Economy.Instance?.Earn(1);
            Destroy(gameObject);
        }
    }
}
