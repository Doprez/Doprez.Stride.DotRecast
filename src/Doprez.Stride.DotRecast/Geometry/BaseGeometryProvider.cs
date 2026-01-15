using Stride.Core;
using Stride.Engine;

namespace Doprez.Stride.DotRecast.Geometry;

[DataContract(Inherited = true)]
public abstract class BaseGeometryProvider
{
    [DataMemberIgnore]
    protected IServiceRegistry Services = null!;

    internal void Initialize(IServiceRegistry registry)
    {
        Services = registry;
        OnInitialize();
    }

    /// <summary>
    /// Called after the provider has been initialized.
    /// </summary>
    protected virtual void OnInitialize()
    {
    }

    /// <summary>
    /// Tries to get the shape information for the geometry. The geometry data should be returned with left handed winding and in world space.
    /// </summary>
    /// <returns></returns>
    public abstract bool TryGetTransformedShapeInfo(Entity entity, out GeometryData shapeData);

    /// <summary>
    /// Tries to get the component that provides the geometry from the entity.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public abstract bool TryGetComponent(Entity entity, out EntityComponent component);
}
