using Content.Server.Power.Components;
using Content.Shared.DockingPowerSharer;
using Content.Shared.Power;

namespace Content.Server.DockingPowerSharer;

/// <summary>
/// This handles...
/// </summary>
public sealed class DockingPowerSharerSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DockingPowerSharerComponent, DockingPowerSharerVoltageChangeMessage>(
            OnVoltageChangeMessage);
    }

    private void OnVoltageChangeMessage(Entity<DockingPowerSharerComponent> ent,
        ref DockingPowerSharerVoltageChangeMessage args)
    {

        ent.Comp.CurrentVoltage = args.newVoltage;
        Dirty(ent);
    }
}


