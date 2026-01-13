# Doprez.Stride.DotRecast
An implementation of DotRecast in Stride with included Bepu Physics support and the ability to provide your own geometry data.

> [!CAUTION]
> This library will break a ton as I continue working with it. But Bepu needed a navigation library since the old Stride implementation was heavily tied to Bullet physics and could not be reused elsewhere.

## Set up
As of January 2026 the set up is as follows:
- Add `DotRecastBoundingBoxComponent` to scene where the nav mesh should generate.
- Add `DotRecastNavigationMeshComponent` to scene that has valid geometry.
 - `EnableDynamicNavigationMesh` tells the processor to update the mesh if new colliders are added otherwise it only builds geometry available when the component is added to the scene.
 - `GeometryProviders` Are the classes that tell the processor how to find and use geometry in the scene. Currently static Bullet and Bepu colliders are supported OOB.

### Custom geometry providers
`BaseGeometryProvider` is the core part of this library that allows users to define what entities can provide colliders that the navigation mesh will use for builds. Setting them up is as easy as telling it what `EntityComponent` is valid and then telling it how to get the vertices and indices from a shape and returning it as `ShapeData` that the mesh builder can read.

