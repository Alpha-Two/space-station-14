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
    [DataField]
    public Voltage CurrentVoltage = Voltage.Medium;

    [DataField]
    public bool IsDischarging = true;

    [ViewVariables]
    public bool TransmittingPower = false;

    [ViewVariables]
    public EntityUid? TransmittingWith;

    [DataField]
    public HashSet<string> ValidInputNodes = new();

    [DataField]
    public HashSet<string> ValidOutputNodes = new();
}

[Serializable, NetSerializable]
public enum DockingPowerSharerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class DockingPowerSharerVoltageChangeMessage(Voltage voltage, bool isGoingOut) : BoundUserInterfaceMessage
{
    public Voltage newVoltage = voltage;
    public bool isGoingOut = isGoingOut;
}
