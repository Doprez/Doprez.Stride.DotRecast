// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Doprez.Stride.DotRecast.Recast.Components;
using Stride.Engine;

namespace Doprez.Stride.DotRecast.Navigation.Processors
{
    internal class DotRecastColliderProcessor : EntityProcessor<NavigationObstacleComponent, StaticColliderData>
    {
        public delegate void CollectionChangedEventHandler(NavigationObstacleComponent component, StaticColliderData data);

        public event CollectionChangedEventHandler ColliderAdded;
        public event CollectionChangedEventHandler ColliderRemoved;

        /// <inheritdoc />
        protected override StaticColliderData GenerateComponentData(Entity entity, NavigationObstacleComponent component)
        {
            return new StaticColliderData { Component = component, };
        }

        /// <inheritdoc />
        protected override bool IsAssociatedDataValid(Entity entity, NavigationObstacleComponent component, StaticColliderData associatedData)
        {
            return component == associatedData.Component;
        }

        /// <inheritdoc />
        protected override void OnEntityComponentAdding(Entity entity, NavigationObstacleComponent component, StaticColliderData data)
        {
            ColliderAdded?.Invoke(component, data);
        }

        /// <inheritdoc />
        protected override void OnEntityComponentRemoved(Entity entity, NavigationObstacleComponent component, StaticColliderData data)
        {
            ColliderRemoved?.Invoke(component, data);
        }
    }
}
