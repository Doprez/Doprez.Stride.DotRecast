using DotRecast.Core;
using DotRecast.Detour.Dynamic.Colliders;
using DotRecast.Recast;
using Stride.Core;
using Stride.Engine;

namespace Doprez.Stride.DotRecast.Geometry;

[DataContract(Inherited = true)]
public abstract class BaseNavigationCollider : IDtCollider
{
    protected int area;
    protected float flagMergeThreshold;

    public abstract float[] Bounds();

    public abstract void Rasterize(RcHeightfield hf, RcContext context);

    public abstract void Initialize(Entity entity, IServiceRegistry registry);

    public virtual GeometryData? GetGeometry(Entity entity, IServiceRegistry services)
    {
        return null;
    }
}
