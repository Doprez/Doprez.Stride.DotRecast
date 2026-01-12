// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Doprez.Stride.DotRecast.Geometry;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Threading;

namespace Doprez.Stride.DotRecast.Navigation
{
    /// <summary>
    /// Incremental navigation mesh builder. 
    /// Builds the navigation mesh in individual tiles
    /// </summary>
    public class NavigationMeshBuilder
    {
        /// <summary>
        /// The logger to send additional information to
        /// </summary>
        public Logger Logger = GlobalLogger.GetLogger(nameof(NavigationMeshBuilder));

        private DotRecastNavigationMesh? _oldNavigationMesh;

        private readonly List<StaticColliderData> _colliders = [];
        private readonly HashSet<Guid> _registeredGuids = [];
        private readonly BaseGeometryProvider[] _geometryProviders = [];

        private readonly IServiceRegistry _services;

        /// <summary>
        /// Initializes the builder, optionally with a previous navigation mesh when building incrementally
        /// </summary>
        /// <param name="oldNavigationMesh">The previous navigation mesh, to allow incremental builds</param>
        public NavigationMeshBuilder(IServiceRegistry services, BaseGeometryProvider[] geometryProviders, DotRecastNavigationMesh oldNavigationMesh = null)
        {
            _oldNavigationMesh = oldNavigationMesh;
            _services = services;
            _geometryProviders = geometryProviders;
        }

        /// <summary>
        /// Adds information about a collider to this builder
        /// </summary>
        /// <remarks>
        /// You can only register a single <see cref="StaticColliderComponent"/> once
        /// </remarks>
        /// <exception cref="InvalidOperationException">When trying to register collider data with the same <see cref="StaticColliderComponent"/> twice</exception>
        /// <param name="colliderData">A collider data object to add</param>
        public void Add(StaticColliderData colliderData)
        {
            lock (_colliders)
            {
                if (_registeredGuids.Contains(colliderData.Component.Id))
                    throw new InvalidOperationException("Duplicate collider added");
                _colliders.Add(colliderData);
                _registeredGuids.Add(colliderData.Component.Id);
            }
        }

        /// <summary>
        /// Removes a specific collider from the builder
        /// </summary>
        /// <param name="colliderData">The collider data object to remove</param>
        public void Remove(StaticColliderData colliderData)
        {
            lock (_colliders)
            {
                if (!_registeredGuids.Contains(colliderData.Component.Id))
                    throw new InvalidOperationException("Trying to remove unregistered collider");
                _colliders.Remove(colliderData);
                _registeredGuids.Remove(colliderData.Component.Id);
            }
        }

        /// <summary>
        /// Performs the build of a navigation mesh
        /// </summary>
        /// <param name="buildSettings">The build settings to pass to recast</param>
        /// <param name="groups">A collection of agent settings to use, this will generate a layer in the navigation mesh for every agent settings in this collection (in the same order)</param>
        /// <param name="includedCollisionGroups">The collision groups that will affect which colliders are considered solid</param>
        /// <param name="boundingBoxes">A collection of bounding boxes to use as the region for which to generate navigation mesh tiles</param>
        /// <param name="cancellationToken">A cancellation token to interrupt the build process</param>
        /// <returns>The build result</returns>
        public NavigationMeshBuildResult  Build(DotRecastNavigationMeshBuildSettings buildSettings, ICollection<DotRecastNavigationMeshGroup> groups, NavMeshLayerGroup includedCollisionGroups,
            ICollection<BoundingBox> boundingBoxes, CancellationToken cancellationToken)
        {
            var lastCache = _oldNavigationMesh?.Cache;
            var result = new NavigationMeshBuildResult();

            if (groups.Count == 0)
            {
                Logger.Warning("No group settings found");
                result.Success = true;
                result.NavigationMesh = new DotRecastNavigationMesh();
                return result;
            }

            if (boundingBoxes.Count == 0)
                Logger.Warning("No bounding boxes found");

            var settingsHash = groups?.ComputeHash() ?? 0;
            settingsHash = (settingsHash * 397) ^ buildSettings.GetHashCode();
            if (lastCache != null && lastCache.SettingsHash != settingsHash)
            {
                // Start from scratch if settings changed
                _oldNavigationMesh = null;
                Logger.Info("Build settings changed, doing a full rebuild");
            }

            // Copy colliders so the collection doesn't get modified
            StaticColliderData[] collidersLocal;
            lock (_colliders)
            {
                collidersLocal = [.. _colliders];
            }

            BuildInput(collidersLocal, includedCollisionGroups);

            // Check if cache was cleared while building the input
            lastCache = _oldNavigationMesh?.Cache;

            // The new navigation mesh that will be created
            result.NavigationMesh = new DotRecastNavigationMesh
            {
                CellSize = buildSettings.CellSize,
                TileSize = buildSettings.TileSize
            };

            // Tile cache for this new navigation mesh
            DotRecastNavigationMeshCache newCache = result.NavigationMesh.Cache = new DotRecastNavigationMeshCache();
            newCache.SettingsHash = settingsHash;

            // Generate global bounding box for planes
            BoundingBox globalBoundingBox = BoundingBox.Empty;
            foreach (var boundingBox in boundingBoxes)
            {
                globalBoundingBox = BoundingBox.Merge(boundingBox, globalBoundingBox);
            }

            // Combine input and collect tiles to build
            GeometryData sceneNavigationMeshInputBuilder = new();
            foreach (var colliderData in collidersLocal)
            {
                if (colliderData == null)
                    continue;

                // Otherwise, skip building these tiles
                sceneNavigationMeshInputBuilder.AppendOther(colliderData.InputBuilder);
                newCache.Add(colliderData.Component, colliderData.InputBuilder, colliderData.ParameterHash);
            }

            // TODO: Generate tile local mesh input data
            var inputVertices = sceneNavigationMeshInputBuilder.Points.ToArray();
            var inputIndices = sceneNavigationMeshInputBuilder.Indices.ToArray();

            // Enumerate over every layer, and build tiles for each of those layers using the provided agent settings
            using (var groupEnumerator = groups.NotNull().GetEnumerator())
            {
                for (int layerIndex = 0; layerIndex < groups.Count; layerIndex++)
                {
                    groupEnumerator.MoveNext();
                    var currentGroup = groupEnumerator.Current;
                    var currentAgentSettings = currentGroup.AgentSettings;

                    if (result.NavigationMesh.LayersInternal.ContainsKey(currentGroup.Id))
                    {
                        Logger.Error($"The same group can't be selected twice: {currentGroup}");
                        return result;
                    }

                    HashSet<Point> tilesToBuild = [];

                    foreach (var colliderData in collidersLocal)
                    {
                        if (colliderData.InputBuilder == null)
                            continue;

                        if (colliderData.Processed)
                        {
                            MarkTiles(colliderData.InputBuilder, ref buildSettings, ref currentAgentSettings, tilesToBuild);
                            if (colliderData.Previous != null)
                                MarkTiles(colliderData.Previous.InputBuilder, ref buildSettings, ref currentAgentSettings, tilesToBuild);
                        }
                    }

                    // Check for removed colliders
                    if (lastCache != null)
                    {
                        foreach (var obj in lastCache.Objects)
                        {
                            if (!newCache.Objects.ContainsKey(obj.Key))
                            {
                                MarkTiles(obj.Value.InputBuilder, ref buildSettings, ref currentAgentSettings, tilesToBuild);
                            }
                        }
                    }

                    // Calculate updated/added bounding boxes
                    foreach (var boundingBox in boundingBoxes)
                    {
                        if (!lastCache?.BoundingBoxes.Contains(boundingBox) ?? true) // In the case of no case, mark all tiles in all bounding boxes to be rebuilt
                        {
                            var tiles = NavigationMeshBuildUtils.GetOverlappingTiles(buildSettings, boundingBox);
                            foreach (var tile in tiles)
                            {
                                tilesToBuild.Add(tile);
                            }
                        }
                    }

                    // Check for removed bounding boxes
                    if (lastCache != null)
                    {
                        foreach (var boundingBox in lastCache.BoundingBoxes)
                        {
                            if (!boundingBoxes.Contains(boundingBox))
                            {
                                var tiles = NavigationMeshBuildUtils.GetOverlappingTiles(buildSettings, boundingBox);
                                foreach (var tile in tiles)
                                {
                                    tilesToBuild.Add(tile);
                                }
                            }
                        }
                    }

                    ConcurrentCollector<Tuple<Point, DotRecastNavigationMeshTile>> builtTiles = new(tilesToBuild.Count);
                    Dispatcher.ForEach(tilesToBuild.ToArray(), tileCoordinate =>
                    {
                        // Allow cancellation while building tiles
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        // Builds the tile, or returns null when there is nothing generated for this tile (empty tile)
                        DotRecastNavigationMeshTile meshTile = BuildTile(tileCoordinate, buildSettings, currentAgentSettings, boundingBoxes,
                            inputVertices, inputIndices);

                        // Add the result to the list of built tiles
                        builtTiles.Add(new Tuple<Point, DotRecastNavigationMeshTile>(tileCoordinate, meshTile));
                    });

                    if (cancellationToken.IsCancellationRequested)
                    {
                        Logger.Warning("Operation cancelled");
                        return result;
                    }

                    // Add layer to the navigation mesh
                    var layer = new NavigationMeshLayer();
                    result.NavigationMesh.LayersInternal.Add(currentGroup.Id, layer);

                    // Copy tiles from the previous build into the current
                    if (_oldNavigationMesh != null && _oldNavigationMesh.LayersInternal.TryGetValue(currentGroup.Id, out var sourceLayer))
                    {
                        foreach (var sourceTile in sourceLayer.Tiles)
                            layer.TilesInternal.Add(sourceTile.Key, sourceTile.Value);
                    }

                    foreach (var p in builtTiles)
                    {
                        if (p.Item2 == null)
                        {
                            // Remove a tile
                            layer.TilesInternal.Remove(p.Item1);
                        }
                        else
                        {
                            // Set or update tile
                            layer.TilesInternal[p.Item1] = p.Item2;
                        }
                    }

                    // Add information about which tiles were updated to the result
                    if (tilesToBuild.Count > 0)
                    {
                        var layerUpdateInfo = new NavigationMeshLayerUpdateInfo
                        {
                            GroupId = currentGroup.Id,
                            UpdatedTiles = [.. tilesToBuild]
                        };
                        result.UpdatedLayers.Add(layerUpdateInfo);
                    }
                }
            }

            // Check for removed layers
            if (_oldNavigationMesh != null)
            {
                var newGroups = groups.ToLookup(x => x.Id);
                foreach (var oldLayer in _oldNavigationMesh.Layers)
                {
                    if (!newGroups.Contains(oldLayer.Key))
                    {
                        var updateInfo = new NavigationMeshLayerUpdateInfo();
                        updateInfo.UpdatedTiles.Capacity = oldLayer.Value.Tiles.Count;

                        foreach (var tile in oldLayer.Value.Tiles)
                        {
                            updateInfo.UpdatedTiles.Add(tile.Key);
                        }

                        result.UpdatedLayers.Add(updateInfo);
                    }
                }
            }

            // Store bounding boxes in new tile cache
            newCache.BoundingBoxes = [.. boundingBoxes];

            // Update navigation mesh
            _oldNavigationMesh = result.NavigationMesh;

            result.Success = true;
            return result;
        }

        private static DotRecastNavigationMeshTile BuildTile(Point tileCoordinate, DotRecastNavigationMeshBuildSettings buildSettings, DotRecastNavigationAgentSettings agentSettings,
            ICollection<BoundingBox> boundingBoxes, Vector3[] inputVertices, int[] inputIndices)
        {
            DotRecastNavigationMeshTile? meshTile = null;

            // Include bounding boxes in tile height range
            BoundingBox tileBoundingBox = NavigationMeshBuildUtils.CalculateTileBoundingBox(buildSettings, tileCoordinate);
            float minimumHeight = float.MaxValue;
            float maximumHeight = float.MinValue;
            bool shouldBuildTile = false;
            foreach (var boundingBox in boundingBoxes)
            {
                if (boundingBox.Intersects(ref tileBoundingBox))
                {
                    maximumHeight = Math.Max(maximumHeight, boundingBox.Maximum.Y);
                    minimumHeight = Math.Min(minimumHeight, boundingBox.Minimum.Y);
                    shouldBuildTile = true;
                }
            }

            NavigationMeshBuildUtils.SnapBoundingBoxToCellHeight(buildSettings, ref tileBoundingBox);

            // Skip tiles that do not overlap with any bounding box
            if (shouldBuildTile)
            {
                // Set tile's minimum and maximum height
                tileBoundingBox.Minimum.Y = minimumHeight;
                tileBoundingBox.Maximum.Y = maximumHeight;

                // Turn build settings into native structure format
                BuildSettings internalBuildSettings = new()
                {
                    // Tile settings
                    BoundingBox = tileBoundingBox,
                    TilePosition = tileCoordinate,
                    TileSize = buildSettings.TileSize,

                    // General build settings
                    CellHeight = buildSettings.CellHeight,
                    CellSize = buildSettings.CellSize,
                    RegionMinArea = buildSettings.MinRegionArea,
                    RegionMergeArea = buildSettings.RegionMergeArea,
                    EdgeMaxLen = buildSettings.MaxEdgeLen,
                    EdgeMaxError = buildSettings.MaxEdgeError,
                    DetailSampleDist = buildSettings.DetailSamplingDistance,
                    DetailSampleMaxError = buildSettings.MaxDetailSamplingError,

                    // Agent settings
                    AgentHeight = agentSettings.Height,
                    AgentRadius = agentSettings.Radius,
                    AgentMaxClimb = agentSettings.MaxClimb,
                    AgentMaxSlope = agentSettings.MaxSlope.Degrees,
                };

                var builder = new NavigationBuilder(internalBuildSettings);
                GeneratedData generatedData = builder.BuildNavmesh(ref inputVertices, ref inputIndices);

                if (generatedData.Success)
                {
                    meshTile = new DotRecastNavigationMeshTile
                    {
                        // Copy the generated navigationMesh data
                        Data = generatedData.NavmeshData
                    };
                }
            }

            return meshTile;
        }

        /// <summary>
        /// Rebuilds outdated triangle data for colliders and recalculates hashes storing everything in StaticColliderData
        /// </summary>
        private void BuildInput(StaticColliderData[] collidersLocal, NavMeshLayerGroup includedCollisionGroups)
        {
            DotRecastNavigationMeshCache? lastCache = _oldNavigationMesh?.Cache;
            
            bool clearCache = false;

            foreach(var colliderData in collidersLocal)
            {
                var entity = colliderData.Component.Entity;

                GeometryData entityNavigationMeshInputBuilder = colliderData.InputBuilder = new();

                // Compute hash of collider and compare it with the previous build if there is one
                colliderData.ParameterHash = NavigationMeshBuildUtils.HashEntityComponent(colliderData.Component, includedCollisionGroups);
                colliderData.Previous = null;
                if (lastCache?.Objects.TryGetValue(colliderData.Component.Id, out colliderData.Previous) ?? false)
                {
                    if (colliderData.Previous.ParameterHash == colliderData.ParameterHash)
                    {
                        // In this case, we don't need to recalculate the geometry for this shape, since it wasn't changed
                        // here we take the triangle mesh from the previous build as the current
                        colliderData.InputBuilder = colliderData.Previous.InputBuilder;
                        colliderData.Processed = true;
                        continue;
                    }
                }

                foreach(var provider in _geometryProviders)
                {
                    if (provider.TryGetTransformedShapeInfo(colliderData.Component.Entity, out var shapeData))
                    {
                        // Append geometry from the provider
                        entityNavigationMeshInputBuilder.AppendOther(shapeData);
                    }
                }
            }

            if (clearCache && _oldNavigationMesh != null)
            {
                _oldNavigationMesh = null;
            }
        }

        /// <summary>
        /// Marks tiles that should be built according to how much their geometry affects the navigation mesh and the bounding boxes specified for building
        /// </summary>
        private static void MarkTiles(GeometryData inputBuilder, ref DotRecastNavigationMeshBuildSettings buildSettings, ref DotRecastNavigationAgentSettings agentSettings, HashSet<Point> tilesToBuild)
        {
            // Extend bounding box for agent size
            BoundingBox boundingBoxToCheck = inputBuilder.BoundingBox;
            NavigationMeshBuildUtils.ExtendBoundingBox(ref boundingBoxToCheck, new Vector3(agentSettings.Radius));

            List<Point> newTileList = NavigationMeshBuildUtils.GetOverlappingTiles(buildSettings, boundingBoxToCheck);
            foreach (Point p in newTileList)
            {
                tilesToBuild.Add(p);
            }
        }
    }
}
