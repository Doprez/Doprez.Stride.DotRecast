// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Doprez.Stride.DotRecast.Geometry;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Doprez.Stride.DotRecast.Navigation
{
    /// <summary>
    /// Represents cached data for a static collider component on an entity
    /// </summary>
    [DataContract(nameof(DotRecastNavigationMeshCachedObject))]
    internal class DotRecastNavigationMeshCachedObject
    {
        /// <summary>
        /// Guid of the collider
        /// </summary>
        public Guid Guid;

        /// <summary>
        /// Hash obtained with <see cref="NavigationMeshBuildUtils.HashEntityComponent"/>
        /// </summary>
        public int ParameterHash;

        /// <summary>
        /// Cached vertex data
        /// </summary>
        public GeometryData InputBuilder;
    }
}
