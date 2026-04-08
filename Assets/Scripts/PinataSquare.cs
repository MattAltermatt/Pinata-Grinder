using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behaviour attached to each individual square within a composite pinata.
/// Tracks health, takes damage, darkens/shrinks and detaches when killed.
/// Dead squares earn money (based on max health) when hitting the death line.
/// Live squares take death line damage; if killed there, earn only $1.
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

    public void Init(Pinata parent, float health, int col, int row)
    {
        _pinata = parent;
        _health = health;
        _maxHealth = health;
        _gridCol = col;
        _gridRow = row;
    }

    public void ReParent(Pinata newParent)
    {
        _pinata = newParent;
    }

    public void TakeDamage(float amount)
    {
        if (_isDead) return;
        _health -= amount;
        if (_health <= 0f)
            Die();
    }

    void Die()
    {
        _isDead = true;
        _sr.color *= 0.5f;
        transform.localScale *= 0.75f;
        gameObject.layer = LayerMask.NameToLayer("DeadSquare");
        _pinata.DetachSquare(this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<DeathLine>(out _)) return;

        if (_isDead)
        {
            CollectWeaponKill();
            return;
        }

        // Live squares take an initial hit on contact, then continuous DPS via Stay
        float dps = GlobalUpgrades.Instance != null
            ? GlobalUpgrades.Instance.DeathLineDamage
            : 1f;
        TakeDamage(dps);
        if (_isDead) { CollectDeathLineKill(); return; }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.TryGetComponent<DeathLine>(out _)) return;

        // Dead squares on the line (killed by weapon while sitting here) — collect them
        if (_isDead)
        {
            CollectWeaponKill();
            return;
        }

        // Live squares take continuous DPS while on the death line
        float dps = GlobalUpgrades.Instance != null
            ? GlobalUpgrades.Instance.DeathLineDamage
            : 1f;
        TakeDamage(dps * Time.deltaTime);
        if (_isDead) { CollectDeathLineKill(); return; }
    }

    void CollectWeaponKill()
    {
        ConfettiBurst.Spawn(transform.position, _sr.color);
        Economy.Instance?.Earn(Mathf.Max(1, Mathf.RoundToInt(_maxHealth * 2f)));
        Destroy(gameObject);
    }

    void CollectDeathLineKill()
    {
        Economy.Instance?.Earn(1);
        Destroy(gameObject);
    }
}
