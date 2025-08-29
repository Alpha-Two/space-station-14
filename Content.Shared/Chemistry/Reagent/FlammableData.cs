namespace Content.Shared.Chemistry.Reagent;

public sealed class FlammableData
{
    [DataField("flammability")]
    public float ContributionToFlammabilityThreshold = 1;

    [DataField]
    public float JoulesPerUnit = 2_000;

    [DataField]
    public float FlameHue = 26 / (float)byte.MaxValue;
}
