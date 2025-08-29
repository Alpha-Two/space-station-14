using Content.Server.Atmos.Components;
using Content.Shared.GroundFlammable;

namespace Content.Server.GroundFlammable;

/// <summary>
/// This handles...
/// </summary>
public sealed class GroundFlammableSystem : SharedGroundFlammableSystem
{
    public override void IgniteTile(Entity<GridAtmosphereComponent?> gridUid, Vector2i position, float temperature, EntityUid? causalUid)
    {

    }
}
