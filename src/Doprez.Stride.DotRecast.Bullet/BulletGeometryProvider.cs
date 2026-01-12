using Doprez.Stride.DotRecast.Geometry;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics.GeometricPrimitives;
using Stride.Physics;

namespace Doprez.Stride.DotRecast.Bullet;

/// <summary>
/// Used to determine static geometry/shapes that can be used with a navigation mesh.
/// </summary>
public class BulletGeometryProvider : BaseGeometryProvider
{
    public CollisionFilterGroups CollidersToInclude { get; set; } = CollisionFilterGroups.AllFilter;

    private readonly Logger _logger = GlobalLogger.GetLogger(nameof(BulletGeometryProvider));

    public override bool EntityHasValidGeometry(Entity entity)
    {
        return entity.Get<StaticColliderComponent>() != null;
    }

    public override bool TryGetTransformedShapeInfo(Entity entity, out GeometryData? shapeData)
    {
        var collidable = entity.Get<StaticColliderComponent>();

        // Only use StaticColliders for the nav mesh build.
        if (collidable is not StaticColliderComponent)
        {
            _logger.Info($"Entity {entity.Name} does not have a {nameof(StaticColliderComponent)}. Only StaticColliders are supported for navigation mesh generation.");
            shapeData = null;
            return false;
        }

        if (!CollidersToInclude.HasFlag(collidable.CollisionGroup))
        {
            _logger.Info($"Entity {entity.Name} is not part of a valid collision layer.");
            shapeData = null;
            return false;
        }

        shapeData = GetBulletGeometry(collidable.ColliderShape, entity.Transform.WorldMatrix);

        if (shapeData == null)
        {
            _logger.Error($"Unsupported collider type for entity {collidable.Entity.Name}. Only CompoundCollider and MeshCollider are supported for navigation mesh generation.");
            shapeData = null;
            return false;
        }

        return true;
    }

    private GeometryData? GetBulletGeometry(ColliderShape colliderhape, Matrix worldTransform)
    {
        var geometry = new GeometryData();

        // Interate through all the colliders shapes while queueing all shapes in compound shapes to process those as well
        Queue<ColliderShape> shapesToProcess = new();
        shapesToProcess.Enqueue(colliderhape);
        while (shapesToProcess.Count > 0)
        {
            var shape = shapesToProcess.Dequeue();
            var shapeType = shape.GetType();
            if (shapeType == typeof(BoxColliderShape))
            {
                var box = (BoxColliderShape)shape;
                var boxDesc = GetColliderShapeDesc<BoxColliderShapeDesc>(box.Description);
                Matrix transform = box.PositiveCenterMatrix * worldTransform;

                var meshData = GeometricPrimitive.Cube.New(boxDesc.Size, toLeftHanded: true);
                geometry.AppendMeshData(meshData, transform);
            }
            else if (shapeType == typeof(SphereColliderShape))
            {
                var sphere = (SphereColliderShape)shape;
                var sphereDesc = GetColliderShapeDesc<SphereColliderShapeDesc>(sphere.Description);
                Matrix transform = sphere.PositiveCenterMatrix * worldTransform;

                var meshData = GeometricPrimitive.Sphere.New(sphereDesc.Radius, toLeftHanded: true);
                geometry.AppendMeshData(meshData, transform);
            }
            else if (shapeType == typeof(CylinderColliderShape))
            {
                var cylinder = (CylinderColliderShape)shape;
                var cylinderDesc = GetColliderShapeDesc<CylinderColliderShapeDesc>(cylinder.Description);
                Matrix transform = cylinder.PositiveCenterMatrix * worldTransform;

                var meshData = GeometricPrimitive.Cylinder.New(cylinderDesc.Height, cylinderDesc.Radius, toLeftHanded: true);
                geometry.AppendMeshData(meshData, transform);
            }
            else if (shapeType == typeof(CapsuleColliderShape))
            {
                var capsule = (CapsuleColliderShape)shape;
                var capsuleDesc = GetColliderShapeDesc<CapsuleColliderShapeDesc>(capsule.Description);
                Matrix transform = capsule.PositiveCenterMatrix * worldTransform;

                var meshData = GeometricPrimitive.Capsule.New(capsuleDesc.Length, capsuleDesc.Radius, toLeftHanded: true);
                geometry.AppendMeshData(meshData, transform);
            }
            else if (shapeType == typeof(ConeColliderShape))
            {
                var cone = (ConeColliderShape)shape;
                var coneDesc = GetColliderShapeDesc<ConeColliderShapeDesc>(cone.Description);
                Matrix transform = cone.PositiveCenterMatrix * worldTransform;

                var meshData = GeometricPrimitive.Cone.New(coneDesc.Radius, coneDesc.Height, toLeftHanded: true);
                geometry.AppendMeshData(meshData, transform);
            }
            else if (shapeType == typeof(StaticPlaneColliderShape))
            {
                var planeShape = (StaticPlaneColliderShape)shape;
                var planeDesc = GetColliderShapeDesc<StaticPlaneColliderShapeDesc>(planeShape.Description);
                Matrix transform = worldTransform;

                Plane plane = new Plane(planeDesc.Normal, planeDesc.Offset)
                {
                    // Pre-Transform plane parameters
                    Normal = Vector3.TransformNormal(planeDesc.Normal, transform)
                };
                plane.Normal.Normalize();
                plane.D += Vector3.Dot(transform.TranslationVector, plane.Normal);

                //colliderData.Planes.Add(plane);
            }
            else if (shapeType == typeof(ConvexHullColliderShape))
            {
                var hull = (ConvexHullColliderShape)shape;
                Matrix transform = hull.PositiveCenterMatrix * worldTransform;

                // Convert hull indices to int
                int[] indices = new int[hull.Indices.Count];
                if (hull.Indices.Count % 3 != 0) throw new InvalidOperationException($"{shapeType} does not consist of triangles");
                for (int i = 0; i < hull.Indices.Count; i += 3)
                {
                    indices[i] = (int)hull.Indices[i];
                    indices[i + 2] = (int)hull.Indices[i + 1]; // NOTE: Reversed winding to create left handed input
                    indices[i + 1] = (int)hull.Indices[i + 2];
                }

                geometry.AppendArrays(hull.Points.ToArray(), indices, transform);
            }
            else if (shapeType == typeof(StaticMeshColliderShape))
            {
                var mesh = (StaticMeshColliderShape)shape;
                Matrix transform = mesh.PositiveCenterMatrix * worldTransform;

                mesh.GetMeshDataCopy(out var verts, out var indices);

                // Convert hull indices to int
                if (indices.Length % 3 != 0) throw new InvalidOperationException($"{shapeType} does not consist of triangles");
                for (int i = 0; i < indices.Length; i += 3)
                {
                    // NOTE: Reversed winding to create left handed input
                    (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
                }

                geometry.AppendArrays(verts, indices, transform);
            }
            else if (shapeType == typeof(HeightfieldColliderShape))
            {
                var heightfield = (HeightfieldColliderShape)shape;

                var halfRange = (heightfield.MaxHeight - heightfield.MinHeight) * 0.5f;
                var offset = -(heightfield.MinHeight + halfRange);
                Matrix transform = Matrix.Translation(new Vector3(0, offset, 0)) * heightfield.PositiveCenterMatrix * worldTransform;

                var width = heightfield.HeightStickWidth - 1;
                var length = heightfield.HeightStickLength - 1;
                var mesh = GeometricPrimitive.Plane.New(width, length, width, length, normalDirection: NormalDirection.UpY, toLeftHanded: true);

                var arrayLength = heightfield.HeightStickWidth * heightfield.HeightStickLength;

                using (heightfield.LockToReadHeights())
                {
                    switch (heightfield.HeightType)
                    {
                        case HeightfieldTypes.Short:
                            if (heightfield.ShortArray == null) continue;
                            for (int i = 0; i < arrayLength; ++i)
                            {
                                mesh.Vertices[i].Position.Y = heightfield.ShortArray[i] * heightfield.HeightScale;
                            }
                            break;
                        case HeightfieldTypes.Byte:
                            if (heightfield.ByteArray == null) continue;
                            for (int i = 0; i < arrayLength; ++i)
                            {
                                mesh.Vertices[i].Position.Y = heightfield.ByteArray[i] * heightfield.HeightScale;
                            }
                            break;
                        case HeightfieldTypes.Float:
                            if (heightfield.FloatArray == null) continue;
                            for (int i = 0; i < arrayLength; ++i)
                            {
                                mesh.Vertices[i].Position.Y = heightfield.FloatArray[i];
                            }
                            break;
                    }
                }

                geometry.AppendMeshData(mesh, transform);
            }
            else if (shapeType == typeof(CompoundColliderShape))
            {
                // Unroll compound collider shapes
                var compound = (CompoundColliderShape)shape;
                for (int i = 0; i < compound.Count; i++)
                {
                    shapesToProcess.Enqueue(compound[i]);
                }
            }
        }

        return geometry;
    }

    /// <summary>
    /// Extract the collider shape description in the case of it being either an inline shape or an asset as shape
    /// </summary>
    private TColliderType GetColliderShapeDesc<TColliderType>(IColliderShapeDesc desc) where TColliderType : class, IColliderShapeDesc
    {
        if (desc is TColliderType direct)
            return direct;
        if (desc is not ColliderShapeAssetDesc asset)
            throw new Exception("Invalid collider shape description");
        return asset.Shape.Descriptions.First() as TColliderType;
    }
}