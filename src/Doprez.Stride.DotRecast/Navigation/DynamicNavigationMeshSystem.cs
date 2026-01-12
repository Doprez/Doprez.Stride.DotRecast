// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Doprez.Stride.DotRecast.Navigation.Components;
using Doprez.Stride.DotRecast.Navigation.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Games;

namespace Doprez.Stride.DotRecast.Navigation
{
    /// <summary>
    /// System that handles building of navigation meshes at runtime
    /// </summary>
    public class DynamicNavigationMeshSystem : GameSystem
    {
        /// <summary>
        /// If <c>true</c>, this will automatically rebuild on addition/removal of static collider components
        /// </summary>
        [DataMember(5)]
        public bool AutomaticRebuild { get; set; } = true;

        private bool pendingRebuild = true;

        private SceneInstance currentSceneInstance;

        private CancellationTokenSource buildTaskCancellationTokenSource;

        private SceneSystem sceneSystem;
        private ScriptSystem scriptSystem;
        private DotRecastNavigationMeshProcessor processor;

        private List<DotRecastNavigationMeshComponent> _navigationMeshComponents = [];

        public DynamicNavigationMeshSystem(IServiceRegistry registry) : base(registry)
        {
            Enabled = true;
            EnabledChanged += OnEnabledChanged;
        }

        /// <summary>
        /// Raised when the navigation mesh for the current scene is updated
        /// </summary>
        public event EventHandler<NavigationMeshUpdatedEventArgs> NavigationMeshUpdated;

        /// <summary>
        /// The most recently built navigation mesh
        /// </summary>
        public DotRecastNavigationMesh CurrentNavigationMesh { get; private set; }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            sceneSystem = Services.GetSafeServiceAs<SceneSystem>();
            scriptSystem = Services.GetSafeServiceAs<ScriptSystem>();
        }

        /// <inheritdoc />
        public override void Update(GameTime gameTime)
        {
            // This system should load from settings before becoming functional
            if (!Enabled)
                return;

            if (currentSceneInstance != sceneSystem?.SceneInstance)
            {
                // ReSharper disable once PossibleNullReferenceException
                UpdateScene(sceneSystem.SceneInstance);
            }

            if (pendingRebuild && currentSceneInstance != null)
            {
                scriptSystem.AddTask(async () =>
                {
                    // TODO EntityProcessors
                    // Currently have to wait a frame for transformations to update
                    // for example when calling Rebuild from the event that a component was added to the scene, this component will not be in the correct location yet
                    // since the TransformProcessor runs the next frame
                    await scriptSystem.NextFrame();
                    await Rebuild();
                });
                pendingRebuild = false;
            }
        }

        /// <summary>
        /// Starts an asynchronous rebuild of the navigation mesh
        /// </summary>
        public async Task<NavigationMeshBuildResult> Rebuild()
        {
            if (currentSceneInstance == null)
                return new NavigationMeshBuildResult();

            // Cancel running build, TODO check if the running build can actual satisfy the current rebuild request and don't cancel in that case
            buildTaskCancellationTokenSource?.Cancel();
            buildTaskCancellationTokenSource = new CancellationTokenSource();

            // Collect bounding boxes
            var boundingBoxProcessor = currentSceneInstance.GetProcessor<DotRecastBoundingBoxProcessor>();
            if (boundingBoxProcessor == null)
                return new NavigationMeshBuildResult();

            List<BoundingBox> boundingBoxes = [];
            foreach (var boundingBox in boundingBoxProcessor.BoundingBoxes)
            {
                boundingBox.Entity.Transform.WorldMatrix.Decompose(out var scale, out Quaternion _, out var translation);
                boundingBoxes.Add(new BoundingBox(translation - boundingBox.Size * scale, translation + boundingBox.Size * scale));
            }

            //foreach(var navMeshComponent in navigationMeshComponents)
            //{
            //    var buildSettings = navMeshComponent.BuildSettings;

            //    var result = Task.Run(() =>
            //    {
            //        // Only have one active build at a time
            //        lock (navMeshComponent.MeshBuilder)
            //        {
            //            return navMeshComponent.MeshBuilder.Build(buildSettings, navMeshComponent.Groups, navMeshComponent.IncludedCollisionGroups, boundingBoxes, buildTaskCancellationTokenSource.Token);
            //        }
            //    });

            //    await result;

            //    FinalizeRebuild(result);

            //    return result.Result;
            //}
            var buildSettings = _navigationMeshComponents[0].BuildSettings;

            var result = Task.Run(() =>
            {
                // Only have one active build at a time
                lock (_navigationMeshComponents[0].MeshBuilder)
                {
                    return _navigationMeshComponents[0].MeshBuilder.Build(buildSettings, _navigationMeshComponents[0].Groups, _navigationMeshComponents[0].IncludedCollisionGroups, boundingBoxes, buildTaskCancellationTokenSource.Token);
                }
            });

            await result;

            FinalizeRebuild(result);

            return result.Result;
        }

        private void FinalizeRebuild(Task<NavigationMeshBuildResult> resultTask)
        {
            var result = resultTask.Result;
            if (result.Success)
            {
                var args = new NavigationMeshUpdatedEventArgs
                {
                    OldNavigationMesh = CurrentNavigationMesh,
                    BuildResult = result,
                };
                CurrentNavigationMesh = result.NavigationMesh;
                NavigationMeshUpdated?.Invoke(this, args);
            }
        }

        private void UpdateScene(SceneInstance newSceneInstance)
        {
            if (currentSceneInstance != null)
            {
                if (processor != null)
                {
                    currentSceneInstance.Processors.Remove(processor);
                    processor.SettingsAdded -= ProcessorOnColliderAdded;
                    processor.SettingsRemoved -= ProcessorOnColliderRemoved;
                }
            }

            // Set the correct scene
            currentSceneInstance = newSceneInstance;

            if (currentSceneInstance != null)
            {
                // Scan for components
                processor = new();
                processor.SettingsAdded += ProcessorOnColliderAdded;
                processor.SettingsRemoved += ProcessorOnColliderRemoved;
                currentSceneInstance.Processors.Add(processor);

                pendingRebuild = true;
            }
        }

        private void ProcessorOnColliderAdded(DotRecastNavigationMeshComponent component)
        {
            _navigationMeshComponents.Add(component);
            if (AutomaticRebuild)
            {
                pendingRebuild = true;
            }
        }

        private void ProcessorOnColliderRemoved(DotRecastNavigationMeshComponent component)
        {
            _navigationMeshComponents.Remove(component);
            if (AutomaticRebuild)
            {
                pendingRebuild = true;
            }
        }

        private void Cleanup()
        {
            UpdateScene(null);

            CurrentNavigationMesh = null;
            NavigationMeshUpdated?.Invoke(this, null);
        }

        private void OnEnabledChanged(object sender, EventArgs eventArgs)
        {
            if (!Enabled)
            {
                Cleanup();
            }
            else
            {
                pendingRebuild = true;
            }
        }
    }

    public class NavigationMeshUpdatedEventArgs : EventArgs
    {
        public DotRecastNavigationMesh OldNavigationMesh;
        public NavigationMeshBuildResult BuildResult;
    }
}
