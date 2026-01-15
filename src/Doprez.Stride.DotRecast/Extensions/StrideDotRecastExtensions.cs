using DotRecast.Core.Numerics;
using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;

namespace Doprez.Stride.DotRecast.Extensions;

public static class StrideDotRecastExtensions
{
    public static RcVec3f ToDotRecastVector(this Vector3 vec)
    {
        return Unsafe.As<Vector3, RcVec3f>(ref vec);
    }

    public static Vector3 ToStrideVector(this RcVec3f vec)
    {
        return Unsafe.As<RcVec3f, Vector3>(ref vec);
    }
}
