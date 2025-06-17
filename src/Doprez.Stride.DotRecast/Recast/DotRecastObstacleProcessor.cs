using Doprez.Stride.DotRecast.Recast.Components;
using Stride.Engine;

namespace Doprez.Stride.DotRecast.Recast;

public class DotRecastObstacleProcessor : EntityProcessor<NavigationObstacleComponent>
{
    public delegate void CollectionChangedEventHandler(NavigationObstacleComponent component);

    public event CollectionChangedEventHandler? ColliderAdded;
    public event CollectionChangedEventHandler? ColliderRemoved;

    /// <inheritdoc />
    protected override void OnEntityComponentAdding(Entity entity, NavigationObstacleComponent component, NavigationObstacleComponent data)
    {
        ColliderAdded?.Invoke(component);
    }

    /// <inheritdoc />
    protected override void OnEntityComponentRemoved(Entity entity, NavigationObstacleComponent component, NavigationObstacleComponent data)
    {
        ColliderRemoved?.Invoke(component);
    }
}
