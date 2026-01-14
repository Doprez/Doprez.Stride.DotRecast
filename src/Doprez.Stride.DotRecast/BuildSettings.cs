using DotRecast.Recast;
using DotRecast.Recast.Toolset;
using Stride.Core;

namespace Doprez.Stride.DotRecast;

[DataContract]
public class BuildSettings
{
    public float CellSize = 0.3f;

    public float CellHeight = 0.2f;

    public float AgentHeight = 2f;

    public float AgentRadius = 0.6f;

    public float AgentMaxClimb = 0.9f;

    public float AgentMaxSlope = 45f;

    public float AgentMaxAcceleration = 8f;

    public int MinRegionSize = 8;

    public int MergedRegionSize = 20;

    public RcPartition PartitionType
    {
        get
        {
            return RcPartitionType.OfValue(Partitioning);
        }
        set
        {
            Partitioning = (int)value;
        }
    }

    [DataMemberIgnore]
    public int Partitioning = RcPartitionType.WATERSHED.Value;

    public bool FilterLowHangingObstacles = true;

    public bool FilterLedgeSpans = true;

    public bool FilterWalkableLowHeightSpans = true;

    public float EdgeMaxLen = 12f;

    public float EdgeMaxError = 1.3f;

    public int VertsPerPoly = 6;

    public float DetailSampleDist = 6f;

    public float DetailSampleMaxError = 1f;

    public int TileSize = 32;

    public RcNavMeshBuildSettings ToRcNavMeshBuildSettings()
    {
        return new RcNavMeshBuildSettings
        {
            cellSize = CellSize,
            cellHeight = CellHeight,
            agentHeight = AgentHeight,
            agentRadius = AgentRadius,
            agentMaxClimb = AgentMaxClimb,
            agentMaxSlope = AgentMaxSlope,
            agentMaxAcceleration = AgentMaxAcceleration,
            minRegionSize = MinRegionSize,
            mergedRegionSize = MergedRegionSize,
            partitioning = Partitioning,
            filterLowHangingObstacles = FilterLowHangingObstacles,
            filterLedgeSpans = FilterLedgeSpans,
            filterWalkableLowHeightSpans = FilterWalkableLowHeightSpans,
            edgeMaxLen = EdgeMaxLen,
            edgeMaxError = EdgeMaxError,
            vertsPerPoly = VertsPerPoly,
            detailSampleDist = DetailSampleDist,
            detailSampleMaxError = DetailSampleMaxError,
            tiled = true,
            tileSize = TileSize
        };
    }
}

