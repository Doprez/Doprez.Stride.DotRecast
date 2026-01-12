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

        private bool _pendingRebuild = true;

        private SceneInstance _currentSceneInstance;

        private CancellationTokenSource _buildTaskCancellationTokenSource;

        private SceneSystem _sceneSystem;
        private ScriptSystem _scriptSystem;
        private DotRecastNavigationMeshProcessor _processor;

        private readonly List<DotRecastNavigationMeshComponent> _navigationMeshComponents = [];

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
            _sceneSystem = Services.GetSafeServiceAs<SceneSystem>();
            _scriptSystem = Services.GetSafeServiceAs<ScriptSystem>();
        }

        /// <inheritdoc />
        public override void Update(GameTime gameTime)
        {
            // This system should load from settings before becoming functional
            if (!Enabled)
                return;

            if (_currentSceneInstance != _sceneSystem?.SceneInstance)
            {
                // ReSharper disable once PossibleNullReferenceException
                UpdateScene(_sceneSystem.SceneInstance);
            }

            if (_pendingRebuild && _currentSceneInstance != null)
            {
                foreach (var navMeshComponent in _navigationMeshComponents)
                {
                    if (navMeshComponent.EnableDynamicNavigationMesh)
                    {
                        _scriptSystem.AddTask(async () =>
                        {
                            // TODO EntityProcessors
                            // Currently have to wait a frame for transformations to update
                            // for example when calling Rebuild from the event that a component was added to the scene, this component will not be in the correct location yet
                            // since the TransformProcessor runs the next frame
                            await _scriptSystem.NextFrame();
                            await Rebuild(navMeshComponent);
                        });
                    }
                }
                _pendingRebuild = false;
            }
        }

        /// <summary>
        /// Starts an asynchronous rebuild of the navigation mesh
        /// </summary>
        public async Task<NavigationMeshBuildResult> Rebuild(DotRecastNavigationMeshComponent navMeshComponent)
        {
            if (_currentSceneInstance == null)
                return new NavigationMeshBuildResult();

            // Cancel running build, TODO check if the running build can actual satisfy the current rebuild request and don't cancel in that case
            _buildTaskCancellationTokenSource?.Cancel();
            _buildTaskCancellationTokenSource = new CancellationTokenSource();

            // Collect bounding boxes
            var boundingBoxProcessor = _currentSceneInstance.GetProcessor<DotRecastBoundingBoxProcessor>();
            if (boundingBoxProcessor == null)
                return new NavigationMeshBuildResult();

            List<BoundingBox> boundingBoxes = [];
            foreach (var boundingBox in boundingBoxProcessor.BoundingBoxes)
            {
                boundingBox.Entity.Transform.WorldMatrix.Decompose(out var scale, out Quaternion _, out var translation);
                boundingBoxes.Add(new BoundingBox(translation - boundingBox.Size * scale, translation + boundingBox.Size * scale));
            }

            var buildSettings = navMeshComponent.BuildSettings;

            var result = Task.Run(() =>
            {
                // Only have one active build at a time
                lock (navMeshComponent.MeshBuilder)
                {
                    return navMeshComponent.MeshBuilder.Build(buildSettings, navMeshComponent.Groups, navMeshComponent.IncludedCollisionGroups, boundingBoxes, _buildTaskCancellationTokenSource.Token);
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
            if (_currentSceneInstance != null)
            {
                if (_processor != null)
                {
                    _currentSceneInstance.Processors.Remove(_processor);
                    _processor.SettingsAdded -= ProcessorOnColliderAdded;
                    _processor.SettingsRemoved -= ProcessorOnColliderRemoved;
                }
            }

            // Set the correct scene
            _currentSceneInstance = newSceneInstance;

            if (_currentSceneInstance != null)
            {
                // Scan for components
                _processor = new();
                _processor.SettingsAdded += ProcessorOnColliderAdded;
                _processor.SettingsRemoved += ProcessorOnColliderRemoved;
                _currentSceneInstance.Processors.Add(_processor);

                _pendingRebuild = true;
            }
        }

        private void ProcessorOnColliderAdded(DotRecastNavigationMeshComponent component)
        {
            _navigationMeshComponents.Add(component);
            if (AutomaticRebuild)
            {
                _pendingRebuild = true;
            }
        }

        private void ProcessorOnColliderRemoved(DotRecastNavigationMeshComponent component)
        {
            _navigationMeshComponents.Remove(component);
            if (AutomaticRebuild)
            {
                _pendingRebuild = true;
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
                _pendingRebuild = true;
            }
        }
    }

    public class NavigationMeshUpdatedEventArgs : EventArgs
    {
        public DotRecastNavigationMesh OldNavigationMesh;
        public NavigationMeshBuildResult BuildResult;
    }
}
