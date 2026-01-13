using Doprez.Stride.DotRecast.Geometry;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Engine;

namespace Doprez.Stride.DotRecast.Navigation.Components;

[DataContract(nameof(DotRecastNavigationMeshComponent))]
[ObjectFactory(typeof(DotRecastNavigationMeshComponentFactory))]
[ComponentCategory("DotRecast")]
public class DotRecastNavigationMeshComponent : EntityComponent
{
    /// <summary>
    /// If set to <c>true</c>, navigation mesh will be updated at runtime. This allows for scene streaming and dynamic obstacles.
    /// </summary>
    /// <userdoc>
    /// Enable dynamic navigation on navigation component.
    /// </userdoc>
    [DataMember(0)]
    public bool EnableDynamicNavigationMesh { get; set; }

    /// <summary>
    /// Build settings used by Recast
    /// </summary>
    /// <userdoc>
    /// Advanced settings for dynamically-generated navigation meshes
    /// </userdoc>
    [DataMember(20)]
    public DotRecastNavigationMeshBuildSettings BuildSettings { get; set; }

    /// <summary>
    /// Settings for agents used with the dynamic navigation mesh
    /// </summary>
    /// <userdoc>
    /// The groups that use the dynamic navigation mesh
    /// </userdoc>
    [DataMember(30)]
    public List<DotRecastNavigationMeshGroup> Groups = [];

    /// <summary>
    /// The geometry providers used to gather collider geometry for navmesh generation
    /// </summary>
    /// <userdoc>
    /// The geometry providers used to gather collider geometry for navmesh generation
    /// </userdoc>
    public List<BaseGeometryProvider> GeometryProviders = [];

    /// <summary>
    /// Used to build the navigation mesh at runtime. Gets set by the processor when the component is added to an entity.
    /// </summary>
    [DataMemberIgnore]
    public NavigationMeshBuilder MeshBuilder = null!;

    /// <summary>
    /// Gets or sets a value indicating whether a rebuild operation is pending.
    /// </summary>
    [DataMemberIgnore]
    public bool PendingRebuild { get; set; }

}

public class DotRecastNavigationMeshComponentFactory : IObjectFactory
{
    public object New(Type type)
    {
        // Initialize build settings
        return new DotRecastNavigationMeshComponent
        {
            EnableDynamicNavigationMesh = false,
            BuildSettings = ObjectFactoryRegistry.NewInstance<DotRecastNavigationMeshBuildSettings>(),
            Groups = [ObjectFactoryRegistry.NewInstance<DotRecastNavigationMeshGroup>()],
        };
    }
}
