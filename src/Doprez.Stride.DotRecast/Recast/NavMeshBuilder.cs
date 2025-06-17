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
    public static DtDynamicNavMesh CreateTiledDynamicNavMesh(RcNavMeshBuildSettings navSettings, IInputGeomProvider geom, CancellationToken cancelToken)
    {
        cancelToken.ThrowIfCancellationRequested();

        RcConfig cfg = new(
            useTiles: true,
            navSettings.tileSize,
            navSettings.tileSize,
            RcConfig.CalcBorder(navSettings.agentRadius, navSettings.cellSize),
            RcPartitionType.OfValue(navSettings.partitioning),
            navSettings.cellSize,
            navSettings.cellHeight,
            navSettings.agentMaxSlope,
            navSettings.agentHeight,
            navSettings.agentRadius,
            navSettings.agentMaxClimb,
            (navSettings.minRegionSize * navSettings.minRegionSize) * navSettings.cellSize * navSettings.cellSize,
            (navSettings.mergedRegionSize * navSettings.mergedRegionSize) * navSettings.cellSize * navSettings.cellSize,
            navSettings.edgeMaxLen,
            navSettings.edgeMaxError,
            navSettings.vertsPerPoly,
            navSettings.detailSampleDist,
            navSettings.detailSampleMaxError,
            navSettings.filterLowHangingObstacles,
            navSettings.filterLedgeSpans,
            navSettings.filterWalkableLowHeightSpans,
            SampleAreaModifications.SAMPLE_AREAMOD_WALKABLE,
            buildMeshDetail: true);

        cancelToken.ThrowIfCancellationRequested();

        var tileBuilder = new TileNavMeshBuilder();
        var builderResults = tileBuilder.Build(geom, navSettings);
        var voxelFile = DtVoxelFile.From(cfg, builderResults.RecastBuilderResults);
        var dtDyanmicNavMesh = new DtDynamicNavMesh(voxelFile);

        return dtDyanmicNavMesh;
    }

    private static int GetTileBits(IInputGeomProvider geom, float cellSize, int tileSize)
    {
        RcRecast.CalcGridSize(geom.GetMeshBoundsMin(), geom.GetMeshBoundsMax(), cellSize, out var sizeX, out var sizeZ);
        int num = (sizeX + tileSize - 1) / tileSize;
        int num2 = (sizeZ + tileSize - 1) / tileSize;
        return Math.Min(DtUtils.Ilog2(DtUtils.NextPow2(num * num2)), 14);
    }
}
