// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Doprez.Stride.DotRecast.Recast.Components;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Doprez.Stride.DotRecast.Navigation
{
    /// <summary>
    /// Utility function for navigation mesh building
    /// </summary>
    public class NavigationMeshBuildUtils
    {
        /// <summary>
        /// Check which tiles overlap a given bounding box
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="boundingBox"></param>
        /// <returns></returns>
        public static List<Point> GetOverlappingTiles(DotRecastNavigationMeshBuildSettings settings, BoundingBox boundingBox)
        {
            List<Point> ret = [];
            float tcs = settings.TileSize * settings.CellSize;
            Vector2 start = boundingBox.Minimum.XZ() / tcs;
            Vector2 end = boundingBox.Maximum.XZ() / tcs;
            Point startTile = new Point(
                (int)Math.Floor(start.X),
                (int)Math.Floor(start.Y));
            Point endTile = new Point(
                (int)Math.Ceiling(end.X),
                (int)Math.Ceiling(end.Y));
            for (int y = startTile.Y; y < endTile.Y; y++)
            {
                for (int x = startTile.X; x < endTile.X; x++)
                {
                    ret.Add(new Point(x, y));
                }
            }
            return ret;
        }
        
        /// <summary>
        /// Snaps a <see cref="BoundingBox"/>'s height according to the given <see cref="DotRecastNavigationMeshBuildSettings"/>
        /// </summary>
        /// <param name="settings">The build settings</param>
        /// <param name="boundingBox">Reference to the bounding box to snap</param>
        public static void SnapBoundingBoxToCellHeight(DotRecastNavigationMeshBuildSettings settings, ref BoundingBox boundingBox)
        {
            // Snap Y to tile height to avoid height differences between tiles
            boundingBox.Minimum.Y = MathF.Floor(boundingBox.Minimum.Y / settings.CellHeight) * settings.CellHeight;
            boundingBox.Maximum.Y = MathF.Ceiling(boundingBox.Maximum.Y / settings.CellHeight) * settings.CellHeight;
        }

        /// <summary>
        /// Calculates X-Z span for a navigation mesh tile. The Y-axis will span from <see cref="float.MinValue"/> to <see cref="float.MaxValue"/>
        /// </summary>
        public static BoundingBox CalculateTileBoundingBox(DotRecastNavigationMeshBuildSettings settings, Point tileCoord)
        {
            float tcs = settings.TileSize * settings.CellSize;
            Vector2 tileMin = new Vector2(tileCoord.X * tcs, tileCoord.Y * tcs);
            Vector2 tileMax = tileMin + new Vector2(tcs);

            BoundingBox boundingBox = BoundingBox.Empty;
            boundingBox.Minimum.X = tileMin.X;
            boundingBox.Minimum.Z = tileMin.Y;
            boundingBox.Maximum.X = tileMax.X;
            boundingBox.Maximum.Z = tileMax.Y;
            boundingBox.Minimum.Y = float.MinValue;
            boundingBox.Maximum.Y = float.MaxValue;

            return boundingBox;
        }

        /// <summary>
        /// Generates a random tangent and binormal for a given normal, 
        /// usefull for creating plane vertices or orienting objects (lookat) where the rotation along the normal doesn't matter
        /// </summary>
        /// <param name="normal"></param>
        /// <param name="tangent"></param>
        /// <param name="binormal"></param>
        public static void GenerateTangentBinormal(Vector3 normal, out Vector3 tangent, out Vector3 binormal)
        {
            tangent = Math.Abs(normal.Y) < 0.01f
                ? new Vector3(normal.Z, normal.Y, -normal.X)
                : new Vector3(-normal.Y, normal.X, normal.Z);
            tangent.Normalize();
            binormal = Vector3.Cross(normal, tangent);
            tangent = Vector3.Cross(binormal, normal);
        }

        /// <summary>
        /// Generates vertices and indices for an infinite size, limited by the <paramref cref="size"/> parameter
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="size">the amount from the origin the plane points are placed</param>
        /// <param name="points"></param>
        /// <param name="inds"></param>
        public static void BuildPlanePoints(ref Plane plane, float size, out Vector3[] points, out int[] inds)
        {
            Vector3 up = plane.Normal;
            GenerateTangentBinormal(up, out var right, out var forward);

            points = new Vector3[4];
            points[0] = -forward * size - right * size + up * plane.D;
            points[1] = -forward * size + right * size + up * plane.D;
            points[2] = forward * size - right * size + up * plane.D;
            points[3] = forward * size + right * size + up * plane.D;

            inds = new int[6];
            // CCW
            inds[0] = 0;
            inds[1] = 2;
            inds[2] = 1;
            inds[3] = 1;
            inds[4] = 2;
            inds[5] = 3;
        }

        /// <summary>
        /// Applies an offset vector to a bounding box to make it bigger or smaller
        /// </summary>
        /// <param name="boundingBox"></param>
        /// <param name="offsets"></param>
        public static void ExtendBoundingBox(ref BoundingBox boundingBox, Vector3 offsets)
        {
            boundingBox.Minimum -= offsets;
            boundingBox.Maximum += offsets;
        }

        /// <summary>
        /// Hashes and entity's transform and it's collider shape settings
        /// </summary>
        /// <param name="collider">The collider to hash</param>
        /// <param name="includedCollisionGroups">The filter group for active collides, 
        ///     which is used to hash if this colliders participates in the navigation mesh build</param>
        /// <returns></returns>
        public static int HashEntityCollider(EntityComponent collider, NavMeshLayerGroup includedCollisionGroups)
        {
            int hash = 0;
            hash = (hash * 397) ^ collider.Entity.Transform.WorldMatrix.GetHashCode();
            //hash = (hash * 397) ^ collider.Enabled.GetHashCode();
            //hash = (hash * 397) ^ CheckColliderFilter(collider, includedCollisionGroups).GetHashCode();
            return hash;
        }
    }
}
