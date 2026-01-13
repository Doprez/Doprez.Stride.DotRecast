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
}
