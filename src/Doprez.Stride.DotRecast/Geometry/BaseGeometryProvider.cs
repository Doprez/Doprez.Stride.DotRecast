using Stride.Core;
using Stride.Engine;

namespace Doprez.Stride.DotRecast.Geometry;

[DataContract(Inherited = true)]
public abstract class BaseGeometryProvider
{
    [DataMemberIgnore]
    public IServiceRegistry Services = null!;

    public void Initialize(IServiceRegistry registry)
    {
        Services = registry;
    }

    /// <summary>
    /// Tries to get the shape information for the geometry. The geometry data should be returned with left handed winding and in world space.
    /// </summary>
    /// <returns></returns>
    public abstract bool TryGetTransformedShapeInfo(Entity entity, out GeometryData? shapeData);
}
