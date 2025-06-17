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
    /// Tries to get the shape information for the geometry.
    /// </summary>
    /// <returns></returns>
    public abstract bool TryGetTransformedShapeInfo(Entity entity, out GeometryData? shapeData);
}
