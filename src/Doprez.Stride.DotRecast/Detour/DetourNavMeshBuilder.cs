using DotRecast.Detour;
using Doprez.Stride.DotRecast.Navigation;

namespace Doprez.Stride.DotRecast.Detour;

public static class DetourNavMeshBuilder
{
    public static DtNavMesh Build(DotRecastNavigationMesh navMesh)
    {
        // Create an empty Detour navmesh and add each tile's prebuilt DtMeshData
        var dtNavMesh = new DtNavMesh();

        if (navMesh == null)
            return dtNavMesh;

        foreach (var layerKvp in navMesh.Layers)
        {
            var layer = layerKvp.Value;
            foreach (var tileKvp in layer.Tiles)
            {
                var tile = tileKvp.Value;
                if (tile?.Data == null)
                    continue;

                // Tile ref will be returned by AddTile, not used currently
                dtNavMesh.AddTile(tile.Data, 0, 0, out _);
            }
        }

        return dtNavMesh;
    }
}
