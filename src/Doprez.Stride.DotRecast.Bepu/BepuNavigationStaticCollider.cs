using Doprez.Stride.DotRecast.Geometry;
using DotRecast.Core;
using DotRecast.Recast;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using System.Runtime.InteropServices;

namespace Doprez.Stride.DotRecast.Bepu;

public class BepuNavigationStaticCollider : BaseNavigationCollider
{
    private float[] _vertices = [0];
    private int[] _triangles = [0];

    public override float[] Bounds()
    {
        float[] bounds = [_vertices[0], _vertices[1], _vertices[2], _vertices[0], _vertices[1], _vertices[2]];
        for (int i = 3; i < _vertices.Length; i += 3)
        {
            bounds[0] = Math.Min(bounds[0], _vertices[i]);
            bounds[1] = Math.Min(bounds[1], _vertices[i + 1]);
            bounds[2] = Math.Min(bounds[2], _vertices[i + 2]);
            bounds[3] = Math.Max(bounds[3], _vertices[i]);
            bounds[4] = Math.Max(bounds[4], _vertices[i + 1]);
            bounds[5] = Math.Max(bounds[5], _vertices[i + 2]);
        }

        return bounds;
    }

    public override void Rasterize(RcHeightfield hf, RcContext context)
    {
        // TODO: check if volume matters for Dotrecast. If it does then we may want to check the shape types and determine the volume.

        for (int i = 0; i < _triangles.Length; i += 3)
        {
            RcRasterizations.RasterizeTriangle(context, _vertices, _triangles[i], _triangles[i + 1], _triangles[i + 2], area,
                hf, (int)MathF.Floor(flagMergeThreshold / hf.ch));
        }
    }

    public override void Initialize(Entity entity, IServiceRegistry services)
    {
        var bepuGeom = new BepuGeometryProvider();
        bepuGeom.Initialize(services);

        if (bepuGeom.TryGetTransformedShapeInfo(entity, out GeometryData? shapeData))
        {
            if(shapeData is null)
            {
                throw new ArgumentNullException($"Failed to get transformed shape info for entity {entity.Name}.");
            }

            Span<Vector3> spanToPoints = CollectionsMarshal.AsSpan(shapeData.Points);
            Span<float> reinterpretedPoints = MemoryMarshal.Cast<Vector3, float>(spanToPoints);

            _vertices = reinterpretedPoints.ToArray();
            _triangles = [.. shapeData.Indices];
        }
    }

    public override GeometryData? GetGeometry(Entity entity, IServiceRegistry services)
    {
        var bepuGeom = new BepuGeometryProvider();
        bepuGeom.Initialize(services);

        if (bepuGeom.TryGetTransformedShapeInfo(entity, out GeometryData? shapeData))
        {
            return shapeData;
        }

        return null;
    }
}
