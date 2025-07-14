using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.DockingPowerSharer;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Shared.Power;

namespace Content.Server.DockingPowerSharer;

/// <summary>
/// This handles...
/// </summary>
public sealed class DockingPowerSharerSystem : EntitySystem
{

    [Dependency] private readonly BatterySystem _battery = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DockingPowerSharerComponent, DockingPowerSharerVoltageChangeMessage>(
            OnVoltageChangeMessage);
        SubscribeLocalEvent<DockingPowerSharerComponent, DockEvent>(OnDock);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var docks = EntityQueryEnumerator<DockingPowerSharerComponent, DockingComponent>();

        while (docks.MoveNext(out var uid, out var ourSharer, out var dockingComp))
        {
            // Not transmitting power? get out of here
            if(!ourSharer.TransmittingPower)
                continue;

            if(!TryComp<BatteryComponent>(uid, out var ourBattery))
                continue;

            // Try find a valid sharer, if not, abort
            if (ourSharer.TransmittingWith == null)
            {
                if (dockingComp.DockedWith == null || !HasComp<DockingPowerSharerComponent>(dockingComp.DockedWith) || !HasComp<BatteryComponent>(dockingComp.DockedWith))
                    continue;
                ourSharer.TransmittingWith = dockingComp.DockedWith;
            }



            if (!TryComp<DockingPowerSharerComponent>(ourSharer.TransmittingWith, out var targetSharer) || !TryComp<BatteryComponent>(ourSharer.TransmittingWith, out var targetBattery))
            {
                ourSharer.TransmittingWith = null;
                continue;
            }

            var difference = ourBattery.CurrentCharge - targetBattery.CurrentCharge;

            difference = Math.Clamp(difference/2, -10000, 10000);



            // Stupid C# not recognising we already checked if it was null
            var targetEnt = ourSharer.TransmittingWith;
            if (targetEnt != null)
            {
                _battery.ChangeCharge((EntityUid)targetEnt, difference, targetBattery);
                _battery.ChangeCharge(uid, -difference, ourBattery);
            }
        }
    }

    private void OnDock(Entity<DockingPowerSharerComponent> ent, ref DockEvent args)
    {
        if (HasComp<DockingPowerSharerComponent>(args.DockingEntityA) && HasComp<DockingPowerSharerComponent>(args.DockingEntityB))
        {
            ent.Comp.TransmittingPower = true;

            // Get the *other* entity
            ent.Comp.TransmittingWith = ent.Owner == args.DockingEntityA ? args.DockingEntityB : args.DockingEntityA;
        }
    }


    private void TryUpdateNodes(Entity<DockingPowerSharerComponent> ent)
    {
        if (!TryComp<NodeContainerComponent>(ent, out var nodeContainer))
            return;

        string? validInputId = null;
        string? validDockId = null;

        foreach (var nodePair in nodeContainer.Nodes)
        {
            if (nodePair.Value.NodeGroupID == (NodeGroupID)ent.Comp.CurrentVoltage)
            {
                if(ent.Comp.ValidInputNodes.Contains(nodePair.Key)) validInputId = nodePair.Key;
                if(ent.Comp.ValidOutputNodes.Contains(nodePair.Key)) validDockId = nodePair.Key;
            }
        }



        if (validInputId == null || validDockId == null)
            return;

        if (TryComp<BatteryChargerComponent>(ent, out var batteryCharger))
        {
            batteryCharger.Voltage = ent.Comp.CurrentVoltage;
            batteryCharger.NodeId = ent.Comp.IsDischarging ? validInputId : validDockId;
            batteryCharger.TryFindAndSetNet();
        }

        if (TryComp<BatteryDischargerComponent>(ent, out var batteryDischarger))
        {
            batteryDischarger.Voltage = ent.Comp.CurrentVoltage;
            batteryDischarger.NodeId = ent.Comp.IsDischarging ? validDockId : validInputId;
            batteryDischarger.TryFindAndSetNet();
        }
    }

    private void OnVoltageChangeMessage(Entity<DockingPowerSharerComponent> ent,
        ref DockingPowerSharerVoltageChangeMessage args)
    {

        ent.Comp.CurrentVoltage = args.newVoltage;
        ent.Comp.IsDischarging = args.isGoingOut;
        TryUpdateNodes(ent);
        Dirty(ent);
    }
}


