using Doprez.Stride.DotRecast.Navigation.Components;
using Stride.Core.Collections;
using Stride.Engine;
using System.Collections.Specialized;

namespace Doprez.Stride.DotRecast.Navigation.Processors;

public class DotRecastNavigationMeshProcessor : EntityProcessor<DotRecastNavigationMeshComponent>
{
    public delegate void CollectionChangedEventHandler(DotRecastNavigationMeshComponent component);

    public event CollectionChangedEventHandler? SettingsAdded;
    public event CollectionChangedEventHandler? SettingsRemoved;

    /// <inheritdoc />
    protected override void OnEntityComponentAdding(Entity entity, DotRecastNavigationMeshComponent component, DotRecastNavigationMeshComponent data)
    {
        component.MeshBuilder = new(Services, [.. component.GeometryProviders]);

        component.Entity.Scene.Entities.CollectionChanged += AddCollider;

        foreach (var otherEntity in component.Entity.Scene.Entities)
        {
            TryAddDataToComponent(otherEntity, component);
        }

        SettingsAdded?.Invoke(component);
    }

    /// <inheritdoc />
    protected override void OnEntityComponentRemoved(Entity entity, DotRecastNavigationMeshComponent component, DotRecastNavigationMeshComponent data)
    {
        SettingsRemoved?.Invoke(component);
    }

    private void AddCollider(object? sender, TrackingCollectionChangedEventArgs e)
    {
        if(e.Action == NotifyCollectionChangedAction.Add)
        {
            if(e.Item is Entity entity)
            {
                foreach(var component in ComponentDatas.Keys)
                {
                    TryAddDataToComponent(entity, component);
                }
            }
        }
    }

    private void TryAddDataToComponent(Entity entity, DotRecastNavigationMeshComponent component)
    {
        foreach (var provider in component.GeometryProviders)
        {
            if(provider.TryGetComponent(entity, out var componentReference))
            {
                StaticColliderData data = new()
                {
                    Component = componentReference,
                };

                component.MeshBuilder.Add(data);
                break;
            }
        }
    }
}
