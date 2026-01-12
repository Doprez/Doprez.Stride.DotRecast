// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Doprez.Stride.DotRecast.Geometry;
using Stride.Engine;

namespace Doprez.Stride.DotRecast.Navigation
{
    /// <summary>
    /// Data associated with static colliders for incremental building of navigation meshes
    /// </summary>
    public class StaticColliderData
    {
        public EntityComponent Component;
        internal int ParameterHash = 0;
        internal bool Processed = false;
        internal GeometryData Geometry;
        internal DotRecastNavigationMeshCachedObject Previous;
    }
}
