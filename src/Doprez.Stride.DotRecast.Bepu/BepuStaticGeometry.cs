using Doprez.Stride.DotRecast.Geometry;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Doprez.Stride.DotRecast.Bepu;

/// <summary>
/// Used to determine static geometry/shapes that can be used with a navigation mesh.
/// </summary>
public class BepuStaticGeometry : BaseGeometryProvider
{
    public CollisionMask CollidersToInclude { get; set; } = CollisionMask.Everything;

    private readonly Logger _logger = GlobalLogger.GetLogger(nameof(BepuStaticGeometry));

    public override bool TryGetTransformedShapeInfo(Entity entity, out GeometryData? shapeData)
    {
        var collidable = entity.Get<CollidableComponent>();

        // Only use StaticColliders for the nav mesh build.
        if (collidable is not StaticComponent)
        {
            _logger.Info($"Entity {entity.Name} does not have a {nameof(StaticComponent)}. Only StaticColliders are supported for navigation mesh generation.");
            shapeData = null;
            return false;
        }

        if (!CollidersToInclude.IsSet(collidable.CollisionLayer))
        {
            _logger.Info($"Entity {entity.Name} is not part of a valid collision layer.");
            shapeData = null;
            return false;
        }

        if(collidable.Collider is CompoundCollider compoundCollider)
        {
            shapeData = GetCompoundGeometry(compoundCollider, collidable.Entity.Transform.WorldMatrix);
            return shapeData != null;
        }
        else if (collidable.Collider is MeshCollider meshCollider)
        {
            shapeData = GetMeshGeometry(meshCollider, collidable.Entity.Transform.WorldMatrix);
            return shapeData != null;
        }

        _logger.Error($"Unsupported collider type {collidable.Collider.GetType().Name} for entity {collidable.Entity.Name}. Only CompoundCollider and MeshCollider are supported for navigation mesh generation.");
        shapeData = null;
        return false;
    }

    private GeometryData? GetCompoundGeometry(CompoundCollider compoundCollider, Matrix worldTransform)
    {
        var geometry = new GeometryData();

        foreach (var collider in compoundCollider.Colliders)
        {
            var position = collider.PositionLocal;
            var rotation = collider.RotationLocal;
            var scale = Vector3.One; // Assuming uniform scale.

            Matrix.Transformation(ref scale, ref rotation, ref position, out var localMatrix);
            worldTransform.Decompose(out _, out Matrix worldMatrix, out var translation);
            // The worldMatrix at this point is only the rotation.
            worldMatrix.TranslationVector = translation; // Apply the translation from the world transform to the world matrix.
            worldTransform = localMatrix * worldMatrix;// Combine local collider matrix and entity world matrix.

            if (collider is BoxCollider boxCollider)
            {
                var box = GeometricPrimitive.Cube.New(boxCollider.Size, toLeftHanded: true);
                geometry.AppendMeshData(box, worldTransform);
            }
            else if(collider is SphereCollider sphereCollider)
            {
                var sphere = GeometricPrimitive.Sphere.New(sphereCollider.Radius, toLeftHanded: true);
                geometry.AppendMeshData(sphere, worldTransform);
            }
            else if (collider is CapsuleCollider capsuleCollider)
            {
                var capsule = GeometricPrimitive.Capsule.New(capsuleCollider.Radius, capsuleCollider.Length, toLeftHanded: true);
                geometry.AppendMeshData(capsule, worldTransform);
            }
            else if (collider is CylinderCollider cylinderCollider)
            {
                var cylinder = GeometricPrimitive.Cylinder.New(cylinderCollider.Radius, cylinderCollider.Length, toLeftHanded: true);
                geometry.AppendMeshData(cylinder, worldTransform);
            }
            else if (collider is ConvexHullCollider convexHullCollider)
            {
                foreach (var mesh in convexHullCollider.Hull.Meshes)
                {
                    foreach (var hull in mesh.Hulls)
                    {
                        // Convert ReadOnlySpan<uint> to int[] before passing to AppendArrays
                        geometry.AppendArrays(hull.Points, hull.Indices.ToArray().Select(i => (int)i).ToArray(), worldTransform);
                    }
                }
            }
            else
            {
                // Unsupported collider type
                _logger.Error($"Unsupported collider type {collider.GetType().Name}.");
                return null;
            }
        }

        return geometry;
    }

    private GeometryData? GetMeshGeometry(MeshCollider meshCollider, Matrix worldTransform)
    {
        var geometry = new GeometryData();
        var (vertices, indices) = GetMeshData(meshCollider.Model, Services.GetSafeServiceAs<IGame>());

        if (vertices.Length == 0 || indices.Length == 0)
        {
            _logger.Error($"Model data was invalid and returned an empty array.");
            return null;
        }

        geometry.AppendArrays(vertices, indices.ToArray().Select(i => (int)i).ToArray(), worldTransform);

        return geometry;
    }

    private static unsafe (Vector3[] vertices, uint[] indices) GetMeshData(Model model, IGame game)
    {
        int totalVertices = 0, totalIndices = 0;
        foreach (var meshData in model.Meshes)
        {
            totalVertices += meshData.Draw.VertexBuffers[0].Count;
            totalIndices += meshData.Draw.IndexBuffer.Count;
        }

        var combinedVertices = new List<Vector3>(totalVertices);
        var combinedIndices = new List<uint>(totalIndices);

        foreach (var meshData in model.Meshes)
        {
            var vBuffer = meshData.Draw.VertexBuffers[0].Buffer;
            var iBuffer = meshData.Draw.IndexBuffer.Buffer;
            byte[] verticesBytes = vBuffer.GetData<byte>(game.GraphicsContext.CommandList);
            byte[] indicesBytes = iBuffer.GetData<byte>(game.GraphicsContext.CommandList);

            if ((verticesBytes?.Length ?? 0) == 0 || (indicesBytes?.Length ?? 0) == 0)
            {
                // returns empty lists if there is an issue
                return (combinedVertices.ToArray(), combinedIndices.ToArray());
            }

            int vertMappingStart = combinedVertices.Count;

            fixed (byte* bytePtr = verticesBytes)
            {
                var vBindings = meshData.Draw.VertexBuffers[0];
                int count = vBindings.Count;
                int stride = vBindings.Declaration.VertexStride;
                for (int i = 0, vHead = vBindings.Offset; i < count; i++, vHead += stride)
                {
                    var pos = *(Vector3*)(bytePtr + vHead);

                    combinedVertices.Add(pos);
                }
            }

            fixed (byte* bytePtr = indicesBytes)
            {
                if (meshData.Draw.IndexBuffer.Is32Bit)
                {
                    foreach (int i in new Span<int>(bytePtr + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                    {
                        combinedIndices.Add((uint)(vertMappingStart + i));
                    }
                }
                else
                {
                    foreach (ushort i in new Span<ushort>(bytePtr + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                    {
                        combinedIndices.Add((uint)(vertMappingStart + i));
                    }
                }
            }
        }

        return (combinedVertices.ToArray(), combinedIndices.ToArray());
    }
}
