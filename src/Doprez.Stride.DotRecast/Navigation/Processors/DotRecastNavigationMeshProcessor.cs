using Doprez.Stride.DotRecast.Navigation.Components;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Games;
using System.Collections.Specialized;

namespace Doprez.Stride.DotRecast.Navigation.Processors;

public class DotRecastNavigationMeshProcessor : EntityProcessor<DotRecastNavigationMeshComponent>
{
    public delegate void CollectionChangedEventHandler(DotRecastNavigationMeshComponent component);

    public event CollectionChangedEventHandler? SettingsAdded;
    public event CollectionChangedEventHandler? SettingsRemoved;

    private readonly Dictionary<EntityComponent, StaticColliderData> _staticColliderDatas = [];

    /// <summary>
    /// Raised when the navigation mesh for the current scene is updated
    /// </summary>
    public event EventHandler<NavigationMeshUpdatedEventArgs> NavigationMeshUpdated;

    /// <summary>
    /// The most recently built navigation mesh
    /// </summary>
    public DotRecastNavigationMesh? CurrentNavigationMesh { get; private set; }

    private SceneInstance? _currentSceneInstance;

    private CancellationTokenSource _buildTaskCancellationTokenSource;

    private SceneSystem _sceneSystem = null!;
    private ScriptSystem _scriptSystem = null!;
    private DotRecastBoundingBoxProcessor _processor = null!;

    private readonly List<DotRecastBoundingBoxComponent> _boundingBoxComponents = [];

    protected override void OnSystemAdd()
    {
        _sceneSystem = Services.GetSafeServiceAs<SceneSystem>();
        _scriptSystem = Services.GetSafeServiceAs<ScriptSystem>();
    }

    protected override void OnSystemRemove()
    {
        _processor.BoundingBoxAdded -= ProcessorOnBoundingBoxAdded;
        _processor.BoundingboxRemoved -= ProcessorOnBoundingBoxRemoved;
        _currentSceneInstance?.Processors.Remove(_processor);
    }

    /// <inheritdoc />
    protected override void OnEntityComponentAdding(Entity entity, DotRecastNavigationMeshComponent component, DotRecastNavigationMeshComponent data)
    {
        component.MeshBuilder = new(Services, [.. component.GeometryProviders]);

        component.Entity.Scene.Entities.CollectionChanged += CollectionChanged;

        foreach(var provider in component.GeometryProviders)
        {
            provider.Initialize(Services);
        }

        foreach (var otherEntity in component.Entity.Scene.Entities)
        {
            if(TryAddDataToComponent(otherEntity, component))
                component.PendingRebuild = true;
        }
        SettingsAdded?.Invoke(component);
    }

    /// <inheritdoc />
    protected override void OnEntityComponentRemoved(Entity entity, DotRecastNavigationMeshComponent component, DotRecastNavigationMeshComponent data)
    {
        SettingsRemoved?.Invoke(component);
    }

    public override void Update(GameTime time)
    {
        // This system should load from settings before becoming functional
        if (!Enabled)
            return;

        if(_processor is null)
        {
            _currentSceneInstance = _sceneSystem.SceneInstance;
            _processor = new();
            _processor.BoundingBoxAdded += ProcessorOnBoundingBoxAdded;
            _processor.BoundingboxRemoved += ProcessorOnBoundingBoxRemoved;
            _currentSceneInstance.Processors.Add(_processor);
        }

        if (_currentSceneInstance != null)
        {
            foreach (var navMeshComponent in ComponentDatas.Values)
            {
                if (navMeshComponent.PendingRebuild)
                {
                    _scriptSystem.AddTask(async () =>
                    {
                        await _scriptSystem.NextFrame();
                        await Rebuild(navMeshComponent);
                    });
                }

                navMeshComponent.PendingRebuild = false;
            }
        }
    }

    private void CollectionChanged(object? sender, TrackingCollectionChangedEventArgs e)
    {
        if(e.Action == NotifyCollectionChangedAction.Add)
        {
            if(e.Item is Entity entity)
            {
                foreach(var component in ComponentDatas.Keys)
                {
                    if (TryAddDataToComponent(entity, component))
                    {
                        if (component.EnableDynamicNavigationMesh)
                        {
                            component.PendingRebuild = true;
                        }
                    }
                }
            }
        }

        if(e.Action == NotifyCollectionChangedAction.Remove)
        {
            if(e.Item is Entity entity)
            {
                foreach(var component in ComponentDatas.Keys)
                {
                    if (TryRemoveCollider(entity, component))
                    {
                        if (component.EnableDynamicNavigationMesh)
                        {
                            component.PendingRebuild = true;
                        }
                    }
                }
            }
        }
    }

    private bool TryAddDataToComponent(Entity entity, DotRecastNavigationMeshComponent component)
    {
        bool componentAdded = false;
        foreach (var provider in component.GeometryProviders)
        {
            if(provider.TryGetComponent(entity, out var componentReference))
            {
                StaticColliderData data = new()
                {
                    Component = componentReference,
                };

                _staticColliderDatas[componentReference] = data;

                component.MeshBuilder.Add(data);
                componentAdded = true;
            }
        }

        return componentAdded;
    }

    private bool TryRemoveCollider(Entity entity, DotRecastNavigationMeshComponent component)
    {
        bool componentRemoved = false;
        foreach (var provider in component.GeometryProviders)
        {
            if (provider.TryGetComponent(entity, out var componentReference))
            {
                if (_staticColliderDatas.TryGetValue(componentReference, out var data))
                {
                    component.MeshBuilder.Remove(data);
                    _staticColliderDatas.Remove(componentReference);
                    componentRemoved = true;
                }
            }
        }
        return componentRemoved;
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
                return navMeshComponent.MeshBuilder.Build(buildSettings, navMeshComponent.Groups, boundingBoxes, _buildTaskCancellationTokenSource.Token);
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

    private void Cleanup()
    {
        CurrentNavigationMesh = null;
        NavigationMeshUpdated?.Invoke(this, null);
    }

    private void OnEnabledChanged(object? sender, EventArgs? eventArgs)
    {
        if (!Enabled)
        {
            Cleanup();
        }
    }

    private void ProcessorOnBoundingBoxAdded(DotRecastBoundingBoxComponent component)
    {
        _boundingBoxComponents.Add(component);
    }

    private void ProcessorOnBoundingBoxRemoved(DotRecastBoundingBoxComponent component)
    {
        _boundingBoxComponents.Remove(component);
    }
}

public class NavigationMeshUpdatedEventArgs : EventArgs
{
    public DotRecastNavigationMesh? OldNavigationMesh;
    public NavigationMeshBuildResult? BuildResult;
}
