using DotRecast.Recast;
using Stride.Core;

namespace Doprez.Stride.DotRecast;

[DataContract]
public class BuildSettings
{
    public NavMeshLayer Layer = NavMeshLayer.Layer1;

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

    public bool Tiled = true;

    public int TileSize = 32;
}

