// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Doprez.Stride.DotRecast.Navigation.Components;
using Stride.Core;
using Stride.Engine;
using Stride.Games;

namespace Doprez.Stride.DotRecast.Navigation.Processors
{
    internal class DotRecastBoundingBoxProcessor : EntityProcessor<DotRecastBoundingBoxComponent>
    {
        public ICollection<DotRecastBoundingBoxComponent> BoundingBoxes => ComponentDatas.Keys;

        protected override void OnSystemAdd()
        {
            // TODO Plugins
            // This is the same kind of entry point as used in PhysicsProcessor
            var gameSystems = Services.GetSafeServiceAs<IGameSystemCollection>();
            var navigationSystem = gameSystems.OfType<DynamicNavigationMeshSystem>().FirstOrDefault();
            if (navigationSystem == null)
            {
                navigationSystem = new DynamicNavigationMeshSystem(Services);
                gameSystems.Add(navigationSystem);
            }
        }
    }
}
