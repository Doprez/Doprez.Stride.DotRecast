// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Doprez.Stride.DotRecast.Navigation.Components;
using Stride.Core.Annotations;
using Stride.Engine;

namespace Doprez.Stride.DotRecast.Navigation.Processors
{
    internal class DotRecastBoundingBoxProcessor : EntityProcessor<DotRecastBoundingBoxComponent>
    {
        public ICollection<DotRecastBoundingBoxComponent> BoundingBoxes => ComponentDatas.Keys;

        public delegate void CollectionChangedEventHandler(DotRecastBoundingBoxComponent component);

        public event CollectionChangedEventHandler? BoundingBoxAdded;
        public event CollectionChangedEventHandler? BoundingboxRemoved;

        internal DotRecastBoundingBoxProcessor() : base(typeof(DotRecastBoundingBoxComponent))
        {
            Order = 100_001;
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] DotRecastBoundingBoxComponent component, [NotNull] DotRecastBoundingBoxComponent data)
        {
            BoundingBoxAdded?.Invoke(component);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] DotRecastBoundingBoxComponent component, [NotNull] DotRecastBoundingBoxComponent data)
        {
            BoundingboxRemoved?.Invoke(component);
        }
    }
}
