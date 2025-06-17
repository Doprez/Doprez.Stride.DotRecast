using DotRecast.Core.Numerics;
using DotRecast.Detour;
using Stride.Core.Mathematics;

namespace Doprez.Stride.DotRecast.Extensions;

public static class NavQueryExtensions
{

    /// <summary>
    /// Find a path from the start polygon to the end polygon.
    /// </summary>
    /// <param name="navQuery"></param>
    /// <param name="startPolyRef"></param>
    /// <param name="endPolyRef"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="filter"></param>
    /// <param name="enableRaycast"></param>
    /// <param name="path"></param>
    /// <param name="smoothPath"></param>
    /// <param name="navSettings"></param>
    /// <returns></returns>
    public static DtStatus FindFollowPath(this DtNavMeshQuery navQuery, long startPolyRef, long endPolyRef, RcVec3f start, RcVec3f end, IDtQueryFilter filter, bool enableRaycast, Span<long> path, ref List<Vector3> smoothPath, PathfindingSettings navSettings)
    {
        if (startPolyRef == 0 || endPolyRef == 0)
        {
            path.Clear();
            smoothPath.Clear();

            return DtStatus.DT_FAILURE;
        }

        path.Clear();
        smoothPath.Clear();

        navQuery.FindPath(startPolyRef, endPolyRef, start, end, filter, path, out var visitedPolysCount, path.Length);

        if (0 >= visitedPolysCount) return DtStatus.DT_FAILURE;

        // Iterate over the path to find smooth path on the detail mesh surface.
        navQuery.ClosestPointOnPoly(startPolyRef, start, out var iterPos, out _);
        navQuery.ClosestPointOnPoly(path[visitedPolysCount - 1], end, out var targetPos, out _);

        const float STEP_SIZE = 0.5f;
        const float SLOP = 0.01f;

        smoothPath.Clear();
        smoothPath.Add(iterPos.ToStrideVector());

        // Move towards target a small advancement at a time until target reached or
        // when ran out of memory to store the path.
        while (0 < visitedPolysCount && smoothPath.Count < navSettings.MaxSmoothing)
        {
            // Find location to steer towards.
            if (!DtPathUtils.GetSteerTarget(navQuery, iterPos, targetPos, SLOP, path, visitedPolysCount, out var steerPos, out var steerPosFlag, out var steerPosRef))
            {
                break;
            }

            bool endOfPath = (steerPosFlag & DtStraightPathFlags.DT_STRAIGHTPATH_END) != 0;
            bool offMeshConnection = (steerPosFlag & DtStraightPathFlags.DT_STRAIGHTPATH_OFFMESH_CONNECTION) != 0;
            // Find movement delta.
            RcVec3f delta = RcVec3f.Subtract(steerPos, iterPos);
            float len = MathF.Sqrt(RcVec3f.Dot(delta, delta));
            // If the steer target is end of path or off-mesh link, do not move past the location.
            if ((endOfPath || offMeshConnection) && len < STEP_SIZE)
            {
                len = 1;
            }
            else
            {
                len = STEP_SIZE / len;
            }

            RcVec3f moveTgt = RcVec.Mad(iterPos, delta, len);

            // Move
            navQuery.MoveAlongSurface(path[0], iterPos, moveTgt, filter, out var result, path, out var nvisited, path.Length);

            iterPos = result;

            visitedPolysCount = DtPathUtils.MergeCorridorStartMoved(path, visitedPolysCount, path.Length, path, nvisited);
            visitedPolysCount = DtPathUtils.FixupShortcuts(path, visitedPolysCount, navQuery);

            var status = navQuery.GetPolyHeight(path[0], result, out var h);
            if (status.Succeeded())
            {
                iterPos.Y = h;
            }

            // Store results.
            if (smoothPath.Count < navSettings.MaxSmoothing)
            {
                smoothPath.Add(iterPos.ToStrideVector());
            }
        }

        return DtStatus.DT_SUCCESS;
    }
}
