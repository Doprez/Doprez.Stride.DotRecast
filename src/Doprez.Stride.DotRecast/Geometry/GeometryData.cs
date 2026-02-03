using System.Buffers;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Doprez.Stride.DotRecast.Geometry;

public class GeometryData : IDisposable
{
    private const int DefaultCapacity = 64;

    public BoundingBox BoundingBox = BoundingBox.Empty;

    private Vector3[] _points = Array.Empty<Vector3>();
    private int[] _indices = Array.Empty<int>();
    private bool _pointsFromPool;
    private bool _indicesFromPool;

    public int PointCount { get; private set; }
    public int IndexCount { get; private set; }

    public ReadOnlySpan<Vector3> Points => new(_points, 0, PointCount);
    public ReadOnlySpan<int> Indices => new(_indices, 0, IndexCount);
    public Vector3[] PointsArray => _points;
    public int[] IndicesArray => _indices;

    /// <summary>
    /// Appends another vertex data builder
    /// </summary>
    /// <param name="other"></param>
    public void AppendOther(GeometryData other)
    {
        var otherPointCount = other.PointCount;
        var otherIndexCount = other.IndexCount;

        if (otherPointCount == 0 && otherIndexCount == 0)
            return;

        EnsurePointCapacity(otherPointCount);
        EnsureIndexCapacity(otherIndexCount);

        var vbase = PointCount;
        if (otherPointCount > 0)
        {
            Array.Copy(other._points, 0, _points, PointCount, otherPointCount);
            PointCount += otherPointCount;

            if (vbase == 0)
            {
                BoundingBox = other.BoundingBox;
            }
            else
            {
                BoundingBox.Merge(ref BoundingBox, ref other.BoundingBox, out BoundingBox);
            }
        }

        if (otherIndexCount > 0)
        {
            var destIndexStart = IndexCount;
            var destIndices = _indices;
            var sourceIndices = other._indices;
            for (var i = 0; i < otherIndexCount; i++)
            {
                destIndices[destIndexStart + i] = sourceIndices[i] + vbase;
            }
            IndexCount += otherIndexCount;
        }
    }

    public void AppendArrays(Vector3[] vertices, int[] indices, Matrix objectTransform, bool isLeftHanded = true)
    {
        AppendArrays(vertices.AsSpan(), indices.AsSpan(), objectTransform, isLeftHanded);
    }

    public void AppendArrays(ReadOnlySpan<Vector3> vertices, ReadOnlySpan<int> indices, Matrix objectTransform, bool isLeftHanded = true)
    {
        EnsurePointCapacity(vertices.Length);
        EnsureIndexCapacity(indices.Length);

        var vbase = PointCount;
        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = Vector3.Transform(vertices[i], objectTransform).XYZ();
            _points[PointCount++] = vertex;
            BoundingBox.Merge(ref BoundingBox, ref vertex, out BoundingBox);
        }

        if (isLeftHanded)
        {
            for (var i = 0; i < indices.Length; i += 3)
            {
                _indices[IndexCount++] = indices[i] + vbase;
                _indices[IndexCount++] = indices[i + 2] + vbase;
                _indices[IndexCount++] = indices[i + 1] + vbase;
            }
        }
        else
        {
            for (var i = 0; i < indices.Length; i++)
            {
                _indices[IndexCount++] = indices[i] + vbase;
            }
        }
    }

    public void AppendArrays(Vector3[] vertices, int[] indices, bool isLeftHanded = true)
    {
        EnsurePointCapacity(vertices.Length);
        EnsureIndexCapacity(indices.Length);

        var vbase = PointCount;
        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = vertices[i];
            _points[PointCount++] = vertex;
            BoundingBox.Merge(ref BoundingBox, ref vertex, out BoundingBox);
        }

        if (isLeftHanded)
        {
            for (var i = 0; i < indices.Length; i += 3)
            {
                _indices[IndexCount++] = indices[i] + vbase;
                _indices[IndexCount++] = indices[i + 2] + vbase;
                _indices[IndexCount++] = indices[i + 1] + vbase;
            }
        }
        else
        {
            for (var i = 0; i < indices.Length; i++)
            {
                _indices[IndexCount++] = indices[i] + vbase;
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
        EnsurePointCapacity(meshData.Vertices.Length);
        EnsureIndexCapacity(meshData.Indices.Length);

        var vbase = PointCount;
        for (var i = 0; i < meshData.Vertices.Length; i++)
        {
            var point = meshData.Vertices[i];
            point.Position = Vector3.Transform(point.Position, objectTransform).XYZ();
            _points[PointCount++] = point.Position;
            BoundingBox.Merge(ref BoundingBox, ref point.Position, out BoundingBox);
        }

        if (meshData.IsLeftHanded)
        {
            for (var i = 0; i < meshData.Indices.Length; i += 3)
            {
                _indices[IndexCount++] = meshData.Indices[i] + vbase;
                _indices[IndexCount++] = meshData.Indices[i + 2] + vbase;
                _indices[IndexCount++] = meshData.Indices[i + 1] + vbase;
            }
        }
        else
        {
            for (var i = 0; i < meshData.Indices.Length; i++)
            {
                _indices[IndexCount++] = meshData.Indices[i] + vbase;
            }
        }
    }

    public void Dispose()
    {
        ReturnPoints();
        ReturnIndices();
        _points = Array.Empty<Vector3>();
        _indices = Array.Empty<int>();
        _pointsFromPool = false;
        _indicesFromPool = false;
        PointCount = 0;
        IndexCount = 0;
        BoundingBox = BoundingBox.Empty;
    }

    private void EnsurePointCapacity(int additional)
    {
        var required = PointCount + additional;
        if (required <= _points.Length)
            return;

        var newSize = CalculateNewSize(_points.Length, required);
        ResizePoints(newSize);
    }

    private void EnsureIndexCapacity(int additional)
    {
        var required = IndexCount + additional;
        if (required <= _indices.Length)
            return;

        var newSize = CalculateNewSize(_indices.Length, required);
        ResizeIndices(newSize);
    }

    private void ResizePoints(int newSize)
    {
        var newArray = ArrayPool<Vector3>.Shared.Rent(newSize);
        if (PointCount > 0)
        {
            Array.Copy(_points, 0, newArray, 0, PointCount);
        }

        ReturnPoints();
        _points = newArray;
        _pointsFromPool = true;
    }

    private void ResizeIndices(int newSize)
    {
        var newArray = ArrayPool<int>.Shared.Rent(newSize);
        if (IndexCount > 0)
        {
            Array.Copy(_indices, 0, newArray, 0, IndexCount);
        }

        ReturnIndices();
        _indices = newArray;
        _indicesFromPool = true;
    }

    private void ReturnPoints()
    {
        if (_pointsFromPool && _points.Length > 0)
        {
            ArrayPool<Vector3>.Shared.Return(_points, clearArray: false);
        }
    }

    private void ReturnIndices()
    {
        if (_indicesFromPool && _indices.Length > 0)
        {
            ArrayPool<int>.Shared.Return(_indices, clearArray: false);
        }
    }

    private static int CalculateNewSize(int current, int required)
    {
        var size = current == 0 ? DefaultCapacity : current;
        while (size < required)
        {
            size *= 2;
        }

        return size;
    }
}
