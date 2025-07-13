using Content.Shared.Power;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DockingPowerSharer;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DockingPowerSharerComponent : Component
{
    [ViewVariables]
    public Voltage CurrentVoltage { get; set; }
}

[Serializable, NetSerializable]
public enum DockingPowerSharerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class DockingPowerSharerVoltageChangeMessage(Voltage voltage) : BoundUserInterfaceMessage
{
    public Voltage newVoltage = voltage;
}
