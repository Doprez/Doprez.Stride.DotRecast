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

    private readonly Dictionary<EntityComponent, StaticColliderData> _staticColliderDatas = [];

    /// <inheritdoc />
    protected override void OnEntityComponentAdding(Entity entity, DotRecastNavigationMeshComponent component, DotRecastNavigationMeshComponent data)
    {
        component.MeshBuilder = new(Services, [.. component.GeometryProviders]);

        component.Entity.Scene.Entities.CollectionChanged += CollectionChanged;

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

    private void CollectionChanged(object? sender, TrackingCollectionChangedEventArgs e)
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

        if(e.Action == NotifyCollectionChangedAction.Remove)
        {
            if(e.Item is Entity entity)
            {
                foreach(var component in ComponentDatas.Keys)
                {
                    RemoveCollider(entity, component);
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

                _staticColliderDatas[componentReference] = data;

                component.MeshBuilder.Add(data);
            }
        }
    }

    private void RemoveCollider(Entity entity, DotRecastNavigationMeshComponent component)
    {
        foreach (var provider in component.GeometryProviders)
        {
            if (provider.TryGetComponent(entity, out var componentReference))
            {
                if (_staticColliderDatas.TryGetValue(componentReference, out var data))
                {
                    component.MeshBuilder.Remove(data);
                    _staticColliderDatas.Remove(componentReference);
                }
            }
        }
    }
}
