using Doprez.Stride.DotRecast.Navigation;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Extensions;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Doprez.Stride.DotRecast.Extensions;

public static class NavMeshExtensions
{
    private const float LayerHeightMultiplier = 0.05f;

    public static Entity CreateDebugEntity(IGame game, DotRecastNavigationMesh navigationMesh, DotRecastNavigationMesh previousNavigationMesh)
    {
        Entity parent = new($"Debug entity for navigation mesh");

        // Create a visual for every layer with a separate color
        using (var layers = navigationMesh.Layers.GetEnumerator())
        {
            while (layers.MoveNext())
            {
                Model model = [];

                var currentLayer = layers.Current.Value;
                var currentId = layers.Current.Key;

                model.Add(CreateDebugMaterial(game, Color.Green));
                model.Add(CreateDebugMaterial(game, Color.GreenYellow));

                foreach (var p in currentLayer.Tiles)
                {
                    bool updated = true;

                    DotRecastNavigationMeshTile tile = p.Value;

                    // Extract vertex data
                    List<Vector3> tileVertexList = [];
                    List<int> tileIndexList = [];
                    if (!tile.GetTileVertices(tileVertexList, tileIndexList))
                        continue;

                    // Check if updated
                    if (previousNavigationMesh != null && previousNavigationMesh.Layers.TryGetValue(currentId, out NavigationMeshLayer? sourceLayer))
                    {
                        DotRecastNavigationMeshTile oldTile = sourceLayer.FindTile(p.Key);
                        if (oldTile != null && oldTile.Data == tile.Data)
                            updated = false;
                    }

                    // Stack layers vertically
                    Vector3 offset = new(0.0f, LayerHeightMultiplier, 0.0f);

                    // Calculate mesh bounding box from navigation mesh points
                    BoundingBox bb = BoundingBox.Empty;

                    List<VertexPositionNormalTexture> meshVertices = [];
                    for (int i = 0; i < tileVertexList.Count; i++)
                    {
                        Vector3 position = tileVertexList[i] + offset;
                        BoundingBox.Merge(ref bb, ref position, out bb);

                        VertexPositionNormalTexture vert = new()
                        {
                            Position = position,
                            Normal = Vector3.UnitY,
                            TextureCoordinate = new Vector2(0.5f, 0.5f)
                        };
                        meshVertices.Add(vert);
                    }

                    MeshDraw draw;
                    using (var meshData = new GeometricMeshData<VertexPositionNormalTexture>([.. meshVertices], [.. tileIndexList], true))
                    {
                        var primitive = new GeometricPrimitive(game.GraphicsDevice, meshData);

                        //ret.GeneratedDynamicPrimitives.Add(primitive);
                        draw = primitive.ToMeshDraw();
                    }

                    Mesh mesh = new()
                    {
                        Draw = draw,
                        MaterialIndex = updated ? 1 : 0,
                        BoundingBox = bb
                    };
                    model.Add(mesh);
                }

                // Create an entity per layer
                var layerEntity = new Entity($"Navigation group {currentId}");

                // Add a new model component
                var modelComponent = new ModelComponent(model);
                layerEntity.Add(modelComponent);
                modelComponent.Enabled = true;
                parent.AddChild(layerEntity);
            }
        }

        return parent;
    }

    public static Material CreateDebugMaterial(IGame game, Color4 color)
    {
        Material navmeshMaterial = Material.New(game.GraphicsDevice, new MaterialDescriptor
        {
            Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor()),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Emissive = new MaterialEmissiveMapFeature(new ComputeColor()),
                }
        });

        Color4 deviceSpaceColor = color.ToColorSpace(game.GraphicsDevice.ColorSpace);
        deviceSpaceColor.A = 0.33f;

        // set the color to the material
        var navmeshMaterialPass = navmeshMaterial.Passes[0];
        navmeshMaterialPass.Parameters.Set(MaterialKeys.DiffuseValue, deviceSpaceColor);
        navmeshMaterialPass.Parameters.Set(MaterialKeys.EmissiveValue, deviceSpaceColor);
        navmeshMaterialPass.Parameters.Set(MaterialKeys.EmissiveIntensity, 1.0f);
        navmeshMaterialPass.HasTransparency = true;

        return navmeshMaterial;
    }
}
