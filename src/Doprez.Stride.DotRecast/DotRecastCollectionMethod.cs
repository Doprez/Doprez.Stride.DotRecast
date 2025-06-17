namespace Doprez.Stride.DotRecast;

public enum DotRecastCollectionMethod
{
    /// <summary>
    /// Collects all entities in the scene of the entity with the <see cref="NavigationMeshComponent"/>"/>
    /// </summary>
    Scene,

    /// <summary>
    /// Collects all children of the entity with the <see cref="NavigationMeshComponent"/>"/>
    /// </summary>
    Children,

    /// <summary>
    /// Collects all entitys with a valid component in a boundingbox volume
    /// </summary>
    BoundingBox,
}
