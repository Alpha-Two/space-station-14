using System.ComponentModel;
using System.Linq;
using Content.Server.Explosion.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Shuttles.Components;
using Content.Shared.DockingPowerSharer;
using Content.Shared.NodeContainer;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server.Power.Nodes;

[DataDefinition]
public sealed partial class DockNode : Node
{
    [Dependency] private readonly MapSystem _mapSystem = default!;

    public override IEnumerable<Node> GetReachableNodes(TransformComponent xform,
        EntityQuery<NodeContainerComponent> nodeQuery,
        EntityQuery<TransformComponent> xformQuery,
        MapGridComponent? grid,
        IEntityManager entMan)
    {
        if (!xform.Anchored || grid == null)
            yield break;


        if (xform.GridUid == null)
            yield break;

        if(!entMan.TryGetComponent<MapGridComponent>(xform.GridUid, out var comp))
            yield break;
        var entities = _mapSystem.GetAnchoredEntities((xform.GridUid.Value, comp), xform.Coordinates);
        entities = entities.Where(entMan.HasComponent<DockingPowerSharerComponent>);
        foreach (var ent in entities)
        {
            if (entMan.TryGetComponent<DockingComponent>(ent, out var dockingComponent))
            {
                var targetEnt = dockingComponent.DockedWith;
                if (targetEnt != null && entMan.TryGetComponent<NodeContainerComponent>(targetEnt, out var targetNodeContainer))
                {
                    foreach (var (_, node) in targetNodeContainer.Nodes)
                    {
                        if(node is DockNode)
                            yield return node;
                    }
                }
            }
        }
    }
}
