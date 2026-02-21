using Doprez.Stride.DotRecast.Navigation;
using Stride.Core.Mathematics;

namespace Doprez.Stride.DotRecast.Detour;

public static class NavMeshPathfinding
{
    public static bool TryFindPath(
        DotRecastNavigationMesh navMesh,
        Vector3 start,
        Vector3 end,
        Span<long> polys,
        ref List<Vector3> path)
    {
        // Placeholder: real Detour query implementation depends on the exact DotRecast.Detour API
        // and how DtNavMeshQuery is used in your codebase.
        path.Clear();
        return false;
    }
}
