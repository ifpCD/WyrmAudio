using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

internal static class Float4x4Extensions
{
    /// <summary>
    /// Gets the translation vector from the matrix.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 GetPosition(this float4x4 m)
    {
        return m.c3.xyz;
    }

    /// <summary>
    /// Extracts the rotation quaternion from the matrix.
    /// Automatically normalizes the axes to handle scaled matrices.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static quaternion GetRotation(this float4x4 m)
    {
        // Normalize columns to strip scale, allowing accurate rotation extraction
        float3 right = math.normalize(m.c0.xyz);
        float3 up = math.normalize(m.c1.xyz);
        float3 forward = math.normalize(m.c2.xyz);

        return math.quaternion(new float3x3(right, up, forward));
    }

    /// <summary>
    /// Extracts the lossy scale from the matrix.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 GetLossyScale(this float4x4 m)
    {
        return math.float3(math.length(m.c0.xyz), math.length(m.c1.xyz), math.length(m.c2.xyz));
    }

    /// <summary>
    /// Returns the inverse of this matrix.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 Inverse(this float4x4 m)
    {
        return math.inverse(m);
    }

    /// <summary>
    /// Transforms a position by this matrix (generic, perspective-correct).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 MultiplyPoint(this float4x4 m, float3 point)
    {
        float3 result = m.c0.xyz * point.x + m.c1.xyz * point.y + m.c2.xyz * point.z + m.c3.xyz;
        float w = m.c0.w * point.x + m.c1.w * point.y + m.c2.w * point.z + m.c3.w;

        // Perspective divide
        return result / w;
    }

    /// <summary>
    /// Transforms a position by this matrix (fast, assumes orthographic/affine).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 MultiplyPoint3x4(this float4x4 m, float3 point)
    {
        return m.c0.xyz * point.x + m.c1.xyz * point.y + m.c2.xyz * point.z + m.c3.xyz;
    }

    /// <summary>
    /// Transforms a direction by this matrix.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 MultiplyVector(this float4x4 m, float3 vector)
    {
        return m.c0.xyz * vector.x + m.c1.xyz * vector.y + m.c2.xyz * vector.z;
    }

    /// <summary>
    /// Get a column of the matrix.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4 GetColumn(this float4x4 m, int index)
    {
        // float4x4 natively implements indexers for columns
        return m[index];
    }

    /// <summary>
    /// Sets a column of the matrix.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetColumn(this ref float4x4 m, int index, float4 column)
    {
        m[index] = column;
    }

    /// <summary>
    /// Returns a row of the matrix.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4 GetRow(this float4x4 m, int index)
    {
        switch (index)
        {
            case 0:
                return math.float4(m.c0.x, m.c1.x, m.c2.x, m.c3.x);
            case 1:
                return math.float4(m.c0.y, m.c1.y, m.c2.y, m.c3.y);
            case 2:
                return math.float4(m.c0.z, m.c1.z, m.c2.z, m.c3.z);
            case 3:
                return math.float4(m.c0.w, m.c1.w, m.c2.w, m.c3.w);
            default:
                throw new IndexOutOfRangeException();
        }
    }

    /// <summary>
    /// Sets a row of the matrix.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetRow(this ref float4x4 m, int index, float4 row)
    {
        switch (index)
        {
            case 0:
                m.c0.x = row.x;
                m.c1.x = row.y;
                m.c2.x = row.z;
                m.c3.x = row.w;
                break;
            case 1:
                m.c0.y = row.x;
                m.c1.y = row.y;
                m.c2.y = row.z;
                m.c3.y = row.w;
                break;
            case 2:
                m.c0.z = row.x;
                m.c1.z = row.y;
                m.c2.z = row.z;
                m.c3.z = row.w;
                break;
            case 3:
                m.c0.w = row.x;
                m.c1.w = row.y;
                m.c2.w = row.z;
                m.c3.w = row.w;
                break;
            default:
                throw new IndexOutOfRangeException();
        }
    }
}
