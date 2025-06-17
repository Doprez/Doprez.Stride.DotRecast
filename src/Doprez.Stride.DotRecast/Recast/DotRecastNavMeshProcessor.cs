using Doprez.Stride.DotRecast.Recast.Components;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;

namespace Doprez.Stride.DotRecast.Recast;

public class DotRecastNavMeshProcessor : EntityProcessor<NavigationMeshComponent>
{
    private SceneSystem _sceneSystem = null!;
    private DotRecastObstacleProcessor _navMeshObstacleProcessor = null!;

    private readonly Queue<NavigationMeshComponent> _addedComponents = new();
    private readonly Queue<NavigationMeshComponent> _removedComponents = new();

    private SceneInstance _currentSceneInstance = null!;

    public DotRecastNavMeshProcessor()
    {
        Order = 50_000;
    }

    protected override void OnSystemAdd()
    {
        Services.AddService(this);

        _sceneSystem = Services.GetSafeServiceAs<SceneSystem>();

        _navMeshObstacleProcessor = new DotRecastObstacleProcessor();
    }

    protected override void OnEntityComponentAdding(Entity entity, [NotNull] NavigationMeshComponent component, [NotNull] NavigationMeshComponent data)
    {
        _addedComponents.Enqueue(component);
    }

    protected override void OnEntityComponentRemoved(Entity entity, [NotNull] NavigationMeshComponent component, [NotNull] NavigationMeshComponent data)
    {
        _removedComponents.Enqueue(component);
    }

    public override void Update(GameTime time)
    {
        if (_currentSceneInstance != _sceneSystem.SceneInstance)
        {
            ChangeScene(_sceneSystem.SceneInstance);
        }

        if (_currentSceneInstance == null)
        {
            return;
        }

        foreach (var component in _addedComponents)
        {
            InitializeNavMeshComponent(component);
        }
        _addedComponents.Clear();

        foreach (var component in ComponentDatas.Values)
        {
            component.Update();
        }
    }

    private static void InitializeNavMeshComponent(NavigationMeshComponent component)
    {
        switch (component.CollectionMethod)
        {
            case DotRecastCollectionMethod.Scene:
                GetObjectsInScene(component);
                break;
            case DotRecastCollectionMethod.Children:
                GetObjectsInChildren(component);
                break;
            case DotRecastCollectionMethod.BoundingBox:
                throw new NotImplementedException("Bounding boxes are not yet supported for nav mesh generation.");
        }
    }

    private void ProcessorOnColliderRemoved(NavigationObstacleComponent component)
    {
        foreach (var navMeshComponent in ComponentDatas.Values)
        {
            navMeshComponent.RemoveObstacle(component);
        }
    }

    private void ProcessorOnColliderAdded(NavigationObstacleComponent component)
    {
        foreach (var navMeshComponent in ComponentDatas.Values)
        {
            navMeshComponent.AddObstacle(component);
        }
    }

    private static void GetObjectsInScene(NavigationMeshComponent component)
    {
        var scene = component.Entity.Scene;

        // Due to how the Transform processor works there shouldnt be a need for recursion here.
        foreach (var entity in scene.Entities)
        {
            component.CheckEntity(entity);
        }
    }

    private void ChangeScene(SceneInstance sceneInstance)
    {
        if (_currentSceneInstance != null)
        {
            _navMeshObstacleProcessor.ColliderAdded -= ProcessorOnColliderAdded;
            _navMeshObstacleProcessor.ColliderRemoved -= ProcessorOnColliderRemoved;
            _sceneSystem.SceneInstance.Processors.Remove(_navMeshObstacleProcessor);
        }

        _currentSceneInstance = sceneInstance;

        if (_currentSceneInstance != null)
        {
            _navMeshObstacleProcessor.ColliderAdded += ProcessorOnColliderAdded;
            _navMeshObstacleProcessor.ColliderRemoved += ProcessorOnColliderRemoved;
            _sceneSystem.SceneInstance.Processors.Add(_navMeshObstacleProcessor);
        }
    }

    private static void GetObjectsInChildren(NavigationMeshComponent component)
    {
        var rootEntity = component.Entity;

        component.CheckEntity(rootEntity);

        RecurseTree(rootEntity, component);

        static void RecurseTree(Entity entity, NavigationMeshComponent component)
        {
            foreach (var child in entity.GetChildren())
            {
                component.CheckEntity(child);
                RecurseTree(child, component);
            }
        }
    }

}

