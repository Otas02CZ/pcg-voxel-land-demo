// FILE: Helpers.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains a set of Vector structures of different data types and length
// TODO: templates, vectorization

using System;
using Vector3 = Godot.Vector3;

namespace PCGVoxelLandscapes.Scripts;

/**
 * 2D integer vector
 */
public struct Vector2Int(int x, int y)
{
    public int x { get; set; } = x;
    public int y { get; set; } = y;
    
    public override bool Equals(object obj)
    {
        if (obj is Vector2Int other)
            return x == other.x && y == other.y;
        return false;
    }
    
    public float DistanceTo(Vector2Int other)
    {
        int dx = x - other.x;
        int dy = y - other.y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(x, y);
    }
    
    public static Vector2Int operator +(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.x + b.x, a.y + b.y);
    }

    public static Vector2Int operator -(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.x - b.x, a.y - b.y);
    }
}

/**
 * 2D short vector
 */
public struct Vector2Short(short x, short y)
{
    public short x { get; set; } = x;
    public short y { get; set; } = y;
    
    public override bool Equals(object obj)
    {
        if (obj is Vector2Short other)
            return x == other.x && y == other.y;
        return false;
    }
    
    public float DistanceTo(Vector2Short other)
    {
        short dx = (short)(x - other.x);
        short dy = (short)(y - other.y);
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(x, y);
    }
    
    public static Vector2Short operator +(Vector2Short a, Vector2Short b)
    {
        return new Vector2Short((short)(a.x + b.x), (short)(a.y + b.y));
    }

    public static Vector2Short operator -(Vector2Short a, Vector2Short b)
    {
        return new Vector2Short((short)(a.x - b.x), (short)(a.y - b.y));
    }
}

/**
 * 3D vector of integers
 */
public struct Vector3Int(int x, int y, int z) : IEquatable<Vector3Int>
{
    public int x { get; set; } = x;
    public int y { get; set; } = y;
    public int z { get; set; } = z;
    
    public override bool Equals(object obj)
    {
        if (obj is Vector3Int other)
            return x == other.x && y == other.y && z == other.z;
        return false;
    }

    public bool Equals(Vector3Int other)
    {
        return x == other.x && y == other.y && z == other.z;
    }
    
    public float DistanceTo(Vector3Int other)
    {
        int dx = x - other.x;
        int dy = y - other.y;
        int dz = z - other.z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(x, y, z);
    }
    
    public static Vector3Int operator +(Vector3Int a, Vector3Int b)
    {
        return new Vector3Int(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static Vector3Int operator -(Vector3Int a, Vector3Int b)
    {
        return new Vector3Int(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public Vector3 ToGodotVector3()
    {
        return new Vector3(x, y, z);
    }
}

/**
 * 3D vector of shorts
 */
public struct Vector3Short(short x, short y, short z)
{
    public short x { get; set; } = x;
    public short y { get; set; } = y;
    public short z { get; set; } = z;
    
    public override bool Equals(object obj)
    {
        if (obj is Vector3Short other)
            return x == other.x && y == other.y && z == other.z;
        return false;
    }
    
    public float DistanceTo(Vector3Short other)
    {
        short dx = (short)(x - other.x);
        short dy = (short)(y - other.y);
        short dz = (short)(z - other.z);
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(x, y, z);
    }

    public static Vector3Short operator +(Vector3Short a, Vector3Short b)
    {
        return new Vector3Short((short)(a.x + b.x), (short)(a.y + b.y), (short)(a.z + b.z));
    }
    
    public static Vector3Short operator -(Vector3Short a, Vector3Short b)
    {
        return new Vector3Short((short)(a.x - b.x), (short)(a.y - b.y), (short)(a.z - b.z));
    }
}

/**
 * 3D vector of doubles
 */
public struct Vector3Double(double x, double y, double z)
{
    public double x { get; set; } = x;
    public double y { get; set; } = y;
    public double z { get; set; } = z;
    
    public override bool Equals(object obj)
    {
        if (obj is Vector3Double other)
            return x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z);
        return false;
    }
    
    public float DistanceTo(Vector3Double other)
    {
        double dx = x - other.x;
        double dy = y - other.y;
        double dz = z - other.z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(x, y, z);
    }
    
    public static Vector3Double operator +(Vector3Double a, Vector3Double b)
    {
        return new Vector3Double(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static Vector3Double operator -(Vector3Double a, Vector3Double b)
    {
        return new Vector3Double(a.x - b.x, a.y - b.y, a.z - b.z);
    }
}
