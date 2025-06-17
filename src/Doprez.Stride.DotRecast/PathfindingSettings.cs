using Stride.Core;

namespace Doprez.Stride.DotRecast;

[DataContract]
[Display("Pathfinding Settings")]
public class PathfindingSettings
{
    /// <summary>
    /// Max amount of smoothing to apply to the path.
    /// </summary>
    public int MaxSmoothing { get; set; } = 128;
}
