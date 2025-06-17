using Stride.Core.Mathematics;
using Stride.Graphics;
using System;

namespace Doprez.Stride.DotRecast.Geometry;

public class GeometryData
{
    public BoundingBox BoundingBox = BoundingBox.Empty;
    public List<Vector3> Points = [];
    public List<int> Indices = [];

    /// <summary>
    /// Appends another vertex data builder
    /// </summary>
    /// <param name="other"></param>
    public void AppendOther(GeometryData other)
    {
        // Copy vertices
        var vbase = Points.Count;
        for (var i = 0; i < other.Points.Count; i++)
        {
            var point = other.Points[i];
            Points.Add(point);
            BoundingBox.Merge(ref BoundingBox, ref point, out BoundingBox);
        }

        // Copy indices with offset applied
        foreach (var index in other.Indices)
            Indices.Add(index + vbase);
    }

    public void AppendArrays(Vector3[] vertices, int[] indices, Matrix objectTransform, bool isLeftHanded = true)
    {
        // Copy vertices
        var vbase = Points.Count;
        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = Vector3.Transform(vertices[i], objectTransform).XYZ();
            Points.Add(vertex);
            BoundingBox.Merge(ref BoundingBox, ref vertex, out BoundingBox);
        }

        // Copy indices with offset applied
        if (isLeftHanded)
        {
            // Copy indices with offset applied
            for (var i = 0; i < indices.Length; i += 3)
            {
                Indices.Add(indices[i] + vbase);
                Indices.Add(indices[i + 2] + vbase);
                Indices.Add(indices[i + 1] + vbase);
            }
        }
        else
        {
            // Copy indices with offset applied
            for (var i = 0; i < indices.Length; i++)
            {
                Indices.Add(indices[i] + vbase);
            }
        }
    }

    public void AppendArrays(ReadOnlySpan<Vector3> vertices, ReadOnlySpan<int> indices, Matrix objectTransform, bool isLeftHanded = true)
    {
        // Copy vertices
        var vbase = Points.Count;
        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = Vector3.Transform(vertices[i], objectTransform).XYZ();
            Points.Add(vertex);
            BoundingBox.Merge(ref BoundingBox, ref vertex, out BoundingBox);
        }

        // Copy indices with offset applied
        if (isLeftHanded)
        {
            // Copy indices with offset applied
            for (var i = 0; i < indices.Length; i += 3)
            {
                Indices.Add(indices[i] + vbase);
                Indices.Add(indices[i + 2] + vbase);
                Indices.Add(indices[i + 1] + vbase);
            }
        }
        else
        {
            // Copy indices with offset applied
            for (var i = 0; i < indices.Length; i++)
            {
                Indices.Add(indices[i] + vbase);
            }
        }
    }

    public void AppendArrays(Vector3[] vertices, int[] indices, bool isLeftHanded = true)
    {
        // Copy vertices
        var vbase = Points.Count;
        for (var i = 0; i < vertices.Length; i++)
        {
            Points.Add(vertices[i]);
            BoundingBox.Merge(ref BoundingBox, ref vertices[i], out BoundingBox);
        }

        // Copy indices with offset applied
        if (isLeftHanded)
        {
            // Copy indices with offset applied
            for (var i = 0; i < indices.Length; i += 3)
            {
                Indices.Add(indices[i] + vbase);
                Indices.Add(indices[i + 2] + vbase);
                Indices.Add(indices[i + 1] + vbase);
            }
        }
        else
        {
            // Copy indices with offset applied
            for (var i = 0; i < indices.Length; i++)
            {
                Indices.Add(indices[i] + vbase);
            }
        }
    }

    /// <summary>
    /// Appends local mesh data transformed with and object transform
    /// </summary>
    /// <param name="meshData"></param>
    /// <param name="objectTransform"></param>
    public void AppendMeshData(GeometricMeshData<VertexPositionNormalTexture> meshData, Matrix objectTransform)
    {
        // Transform box points
        var vbase = Points.Count;
        for (var i = 0; i < meshData.Vertices.Length; i++)
        {
            var point = meshData.Vertices[i];
            point.Position = Vector3.Transform(point.Position, objectTransform).XYZ();
            Points.Add(point.Position);
            BoundingBox.Merge(ref BoundingBox, ref point.Position, out BoundingBox);
        }

        if (meshData.IsLeftHanded)
        {
            // Copy indices with offset applied
            for (var i = 0; i < meshData.Indices.Length; i += 3)
            {
                Indices.Add(meshData.Indices[i] + vbase);
                Indices.Add(meshData.Indices[i + 2] + vbase);
                Indices.Add(meshData.Indices[i + 1] + vbase);
            }
        }
        else
        {
            // Copy indices with offset applied
            for (var i = 0; i < meshData.Indices.Length; i++)
            {
                Indices.Add(meshData.Indices[i] + vbase);
            }
        }
    }
}
