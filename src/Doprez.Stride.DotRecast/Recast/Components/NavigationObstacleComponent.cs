using Doprez.Stride.DotRecast.Geometry;
using DotRecast.Detour.Dynamic.Colliders;
using Stride.Core;
using Stride.Engine;

namespace Doprez.Stride.DotRecast.Recast.Components;

[DataContract]
[ComponentCategory("DotRecast")]
public class NavigationObstacleComponent : StartupScript
{

    /// <summary>
    /// Determines the navigation layers that this collider will affect.
    /// </summary>
    public NavMeshLayerGroup NavigationLayers { get; set; }

    /// <summary>
    /// The class used to gather the collider information being passed to the dynamic nav mesh.
    /// </summary>
    public required BaseNavigationCollider Collider { get; set; }

    public IDtCollider GetCollider()
    {
        Collider.Initialize(Entity, Services);
        return Collider;
    }
}
