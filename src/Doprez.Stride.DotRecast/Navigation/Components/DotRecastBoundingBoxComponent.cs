// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Doprez.Stride.DotRecast.Navigation.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Doprez.Stride.DotRecast.Navigation.Components
{
    /// <summary>
    /// A three dimensional bounding box  using the scale of the owning entity as the box extent. This is used to limit the area in which navigation meshes are generated
    /// </summary>
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(DotRecastBoundingBoxProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [Display("DotRecast bounding box")]
    [ComponentCategory("Navigation")]
    public class DotRecastBoundingBoxComponent : EntityComponent
    {
        /// <summary>
        /// The size of one edge of the bounding box
        /// </summary>
        [DataMember(0)]
        public Vector3 Size { get; set; } = Vector3.One;
    }
}
