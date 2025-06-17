using DotRecast.Detour;
using DotRecast.Recast.Geom;
using DotRecast.Recast;
using DotRecast.Recast.Toolset.Builder;
using DotRecast.Recast.Toolset;
using DotRecast.Detour.Dynamic.Io;
using DotRecast.Detour.Dynamic;

namespace Doprez.Stride.DotRecast.Recast;

public class NavMeshBuilder
{
    public static DtDynamicNavMesh CreateTiledDynamicNavMesh(BuildSettings navSettings, IInputGeomProvider geom, CancellationToken cancelToken)
    {
        cancelToken.ThrowIfCancellationRequested();

        RcConfig cfg = new(
            useTiles: true,
            navSettings.TileSize,
            navSettings.TileSize,
            RcConfig.CalcBorder(navSettings.AgentRadius, navSettings.CellSize),
            RcPartitionType.OfValue(navSettings.Partitioning),
            navSettings.CellSize,
            navSettings.CellHeight,
            navSettings.AgentMaxSlope,
            navSettings.AgentHeight,
            navSettings.AgentRadius,
            navSettings.AgentMaxClimb,
            (navSettings.MinRegionSize * navSettings.MinRegionSize) * navSettings.CellSize * navSettings.CellSize,
            (navSettings.MergedRegionSize * navSettings.MergedRegionSize) * navSettings.CellSize * navSettings.CellSize,
            navSettings.EdgeMaxLen,
            navSettings.EdgeMaxError,
            navSettings.VertsPerPoly,
            navSettings.DetailSampleDist,
            navSettings.DetailSampleMaxError,
            navSettings.FilterLowHangingObstacles,
            navSettings.FilterLedgeSpans,
            navSettings.FilterWalkableLowHeightSpans,
            SampleAreaModifications.SAMPLE_AREAMOD_WALKABLE,
            buildMeshDetail: true);

        cancelToken.ThrowIfCancellationRequested();

        var tileBuilder = new TileNavMeshBuilder();
        var builderResults = tileBuilder.Build(geom, navSettings.ToRcNavMeshBuildSettings());
        var voxelFile = DtVoxelFile.From(cfg, builderResults.RecastBuilderResults);
        var dtDyanmicNavMesh = new DtDynamicNavMesh(voxelFile);

        return dtDyanmicNavMesh;
    }
}
