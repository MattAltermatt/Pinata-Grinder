using UnityEngine;

/// <summary>
/// A single missile launcher turret that rotates toward targets and fires Missile projectiles.
/// Managed by MissileGroup — not a Weapon subclass itself.
/// Uses lead targeting to predict where falling pinatas will be.
/// </summary>
public class MissileLauncher : MonoBehaviour
{
    [SerializeField, HideInInspector] private Transform stopper;
    [SerializeField, HideInInspector] private bool initialized;
    [SerializeField, HideInInspector] private float orbitRadius;

    private float _fireInterval = 5f;
    private float _damage = 5f;
    private float _blastRadius = 0.4f;
    private float _missileSpeed = 1.5f;
    private float _homingStrength = 0f;

    private Pinata _target;
    private float _reloadTimer;
    private bool _isReloading;

    public void Init(Vector2 stopperCenter, float stopperRadius)
    {
        orbitRadius = stopperRadius;
        initialized = true;

        transform.position = (Vector3)stopperCenter;
        transform.localScale = Vector3.one * (stopperRadius * 2f);

        gameObject.layer = LayerMask.NameToLayer("Weapon");

        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GameField.MissileLauncherSprite();
        sr.sortingOrder = 4;
    }

    public void SetStopper(Transform stopperTransform) => stopper = stopperTransform;
    public void SetFireInterval(float interval) => _fireInterval = interval;
    public void SetDamage(float dmg) => _damage = dmg;
    public void SetBlastRadius(float r) => _blastRadius = r;
    public void SetMissileSpeed(float spd) => _missileSpeed = spd;
    public void SetHomingStrength(float str) => _homingStrength = str;

    void Update()
    {
        if (!initialized || stopper == null) return;
        transform.position = stopper.position;

        // Reload timer
        if (_isReloading)
        {
            _reloadTimer -= Time.deltaTime;
            if (_reloadTimer <= 0f)
                _isReloading = false;
            return;
        }

        // Acquire target if needed
        if (_target == null || _target.AliveCount == 0)
            AcquireTarget();

        if (_target == null) return;

        // Compute lead targeting intercept direction toward center of mass
        Vector2 interceptDir = ComputeLeadDirection();

        // Rotate launcher toward intercept point
        float targetAngle = Mathf.Atan2(interceptDir.y, interceptDir.x) * Mathf.Rad2Deg;
        var targetRot = Quaternion.Euler(0f, 0f, targetAngle);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, targetRot, 360f * Time.deltaTime);

        // Fire when aimed within 10 degrees
        float aimError = Quaternion.Angle(transform.rotation, targetRot);
        if (aimError <= 10f)
            Fire();
    }

    void AcquireTarget()
    {
        var pinatas = FindObjectsByType<Pinata>(FindObjectsInactive.Exclude);
        float bestScore = float.MaxValue;
        Pinata best = null;
        Vector2 aimDir = transform.right;

        for (int i = 0; i < pinatas.Length; i++)
        {
            if (pinatas[i].AliveCount == 0) continue;
            Vector2 center = pinatas[i].CenterOfMass();
            Vector2 toTarget = center - (Vector2)transform.position;
            float dist = toTarget.magnitude;
            float angleDiff = Vector2.Angle(aimDir, toTarget.normalized);
            // Prefer larger pinatas (more squares = higher value target) and closer ones
            float sizeBonus = pinatas[i].AliveCount * 0.1f;
            float score = dist + angleDiff * 0.02f - sizeBonus;

            if (score < bestScore)
            {
                bestScore = score;
                best = pinatas[i];
            }
        }
        _target = best;
    }

    /// <summary>
    /// Solves the quadratic intercept equation to predict where the target will be
    /// when the missile arrives. Falls back to direct aim if no solution exists.
    /// </summary>
    Vector2 ComputeLeadDirection()
    {
        Vector2 launcherPos = transform.position;
        Vector2 targetPos = _target.CenterOfMass();

        // Get target velocity from Pinata's Rigidbody2D
        Vector2 targetVel = Vector2.zero;
        var rb = _target.GetComponent<Rigidbody2D>();
        if (rb != null)
            targetVel = rb.linearVelocity;

        Vector2 relPos = targetPos - launcherPos;

        // Quadratic: |relPos + targetVel*t|^2 = (missileSpeed*t)^2
        float a = targetVel.sqrMagnitude - _missileSpeed * _missileSpeed;
        float b = 2f * Vector2.Dot(relPos, targetVel);
        float c = relPos.sqrMagnitude;

        float t = 0f;
        float discriminant = b * b - 4f * a * c;

        if (Mathf.Abs(a) < 0.001f)
        {
            // Linear case: missile speed roughly equals target speed
            if (Mathf.Abs(b) > 0.001f)
                t = Mathf.Max(0f, -c / b);
        }
        else if (discriminant >= 0f)
        {
            float sqrtD = Mathf.Sqrt(discriminant);
            float t1 = (-b - sqrtD) / (2f * a);
            float t2 = (-b + sqrtD) / (2f * a);

            if (t1 > 0.01f && t2 > 0.01f) t = Mathf.Min(t1, t2);
            else if (t1 > 0.01f) t = t1;
            else if (t2 > 0.01f) t = t2;
        }

        t = Mathf.Clamp(t, 0f, 5f);

        Vector2 interceptPoint = targetPos + targetVel * t;
        return (interceptPoint - launcherPos).normalized;
    }

    void Fire()
    {
        var go = new GameObject("Missile");
        go.transform.position = transform.position;
        go.transform.rotation = transform.rotation;

        var missile = go.AddComponent<Missile>();
        missile.Init(
            transform.right,
            _missileSpeed,
            _damage,
            _blastRadius,
            _homingStrength,
            _target   // Pinata reference for homing toward center mass
        );

        _isReloading = true;
        _reloadTimer = _fireInterval;
        _target = null;
    }
}
