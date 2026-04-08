public enum DamageType { Physical, Energy, Explosive }

public enum PinataVariantType { Basic, Armored, Shielded, Swift, Heavy }

public struct PinataVariantDef
{
    public PinataVariantType Type;
    public float PhysicalMult;
    public float EnergyMult;
    public float ExplosiveMult;
    public float GravityMult;
    public float MassMult;
    public float RewardMult;
    public float ShieldFraction; // fraction of maxHealth as shield HP (0 = none)
    public float Hue;            // -1 = random
    public float Saturation;
    public float Value;
}

public static class PinataVariantDefs
{
    public static readonly PinataVariantDef[] All =
    {
        new() // Basic
        {
            Type = PinataVariantType.Basic,
            PhysicalMult = 1f, EnergyMult = 1f, ExplosiveMult = 1f,
            GravityMult = 1f, MassMult = 1f, RewardMult = 1f,
            ShieldFraction = 0f, Hue = -1f, Saturation = 0.45f, Value = 0.97f
        },
        new() // Armored
        {
            Type = PinataVariantType.Armored,
            PhysicalMult = 0.3f, EnergyMult = 1f, ExplosiveMult = 1f,
            GravityMult = 1f, MassMult = 1.5f, RewardMult = 1.5f,
            ShieldFraction = 0f, Hue = 0f, Saturation = 0.05f, Value = 0.6f
        },
        new() // Shielded
        {
            Type = PinataVariantType.Shielded,
            PhysicalMult = 1f, EnergyMult = 1f, ExplosiveMult = 1f,
            GravityMult = 1f, MassMult = 1f, RewardMult = 1.3f,
            ShieldFraction = 0.5f, Hue = 0.6f, Saturation = 0.6f, Value = 0.95f
        },
        new() // Swift
        {
            Type = PinataVariantType.Swift,
            PhysicalMult = 1f, EnergyMult = 1f, ExplosiveMult = 1f,
            GravityMult = 2f, MassMult = 0.5f, RewardMult = 1.4f,
            ShieldFraction = 0f, Hue = 0.15f, Saturation = 0.2f, Value = 1f
        },
        new() // Heavy
        {
            Type = PinataVariantType.Heavy,
            PhysicalMult = 1f, EnergyMult = 1f, ExplosiveMult = 1f,
            GravityMult = 0.6f, MassMult = 3f, RewardMult = 1.6f,
            ShieldFraction = 0f, Hue = 0.75f, Saturation = 0.3f, Value = 0.5f
        }
    };

    public static PinataVariantDef Get(PinataVariantType type) => All[(int)type];
}
