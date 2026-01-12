using Doprez.Stride.DotRecast.Geometry;
using Stride.Core;
using Stride.Engine;

namespace Doprez.Stride.DotRecast.Navigation.Components;

[DataContract(nameof(DotRecastNavigationMeshComponent))]
[ComponentCategory("Navigation")]
public class DotRecastNavigationMeshComponent : EntityComponent
{
    /// <summary>
    /// If set to <c>true</c>, navigation mesh will be updated at runtime. This allows for scene streaming and dynamic obstacles.
    /// </summary>
    /// <userdoc>
    /// Enable dynamic navigation on navigation component.
    /// </userdoc>
    [DataMember(0)]
    [Display("Enabled", "Dynamic navigation mesh")]
    public bool EnableDynamicNavigationMesh { get; set; }

    /// <summary>
    /// Collision filter that indicates which colliders are used in navmesh generation
    /// </summary>
    /// <userdoc>
    /// Set which collision groups dynamically-generated navigation meshes use
    /// </userdoc>
    [DataMember(10)]
    [Display(category: "Dynamic navigation mesh")]
    public NavMeshLayerGroup IncludedCollisionGroups { get; set; } = NavMeshLayerGroup.All;

    /// <summary>
    /// Build settings used by Recast
    /// </summary>
    /// <userdoc>
    /// Advanced settings for dynamically-generated navigation meshes
    /// </userdoc>
    [DataMember(20)]
    [Display(category: "Dynamic navigation mesh")]
    public DotRecastNavigationMeshBuildSettings BuildSettings { get; set; }

    /// <summary>
    /// Settings for agents used with the dynamic navigation mesh
    /// </summary>
    /// <userdoc>
    /// The groups that use the dynamic navigation mesh
    /// </userdoc>
    [DataMember(30)]
    public List<DotRecastNavigationMeshGroup> Groups = [];

    [DataMemberIgnore]
    public NavigationMeshBuilder MeshBuilder = null!;

    /// <summary>
    /// The geometry providers used to gather collider geometry for navmesh generation
    /// </summary>
    public List<BaseGeometryProvider> GeometryProviders = [];
}
