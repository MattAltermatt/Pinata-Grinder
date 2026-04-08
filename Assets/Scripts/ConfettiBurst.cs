using UnityEngine;

/// <summary>
/// Spawns a one-shot confetti particle burst at a world position,
/// then self-destructs after the particles have faded.
/// </summary>
public class ConfettiBurst : MonoBehaviour
{
    public static void Spawn(Vector3 position, Color squareColor)
    {
        var go = new GameObject("ConfettiBurst");
        go.transform.position = position;
        go.AddComponent<ConfettiBurst>().Initialize(squareColor);
    }

    void Initialize(Color squareColor)
    {
        var ps  = gameObject.AddComponent<ParticleSystem>();

        // URP requires an explicit particle material — default-particle is not URP-compatible
        // and falls back to the magenta error shader
        var psr = gameObject.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        // ── Main ──────────────────────────────────────────────────────────────
        var main = ps.main;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(1f, 2.5f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(1.5f, 6f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.06f, 0.2f);
        main.startRotation   = new ParticleSystem.MinMaxCurve(
            -180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);
        main.startColor      = squareColor;
        main.gravityModifier = 0.4f;
        main.maxParticles    = 15;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // ── Emission (single burst) ───────────────────────────────────────────
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

        // ── Shape: sphere so particles fly out in all directions ──────────────
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.3f;

        // ── Fade out over lifetime ────────────────────────────────────────────
        // Alpha-only fade — white keys leave startColor unchanged, alpha goes 1→0
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        // ── Rotation over lifetime for a tumbling confetti look ───────────────
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z       = new ParticleSystem.MinMaxCurve(-270f * Mathf.Deg2Rad, 270f * Mathf.Deg2Rad);

        ps.Play();
        Destroy(gameObject, 3f);
    }
}
