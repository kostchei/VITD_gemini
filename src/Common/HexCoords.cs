using System;
using System.Collections.Generic;
using Godot;

namespace VastDark.Common;

public struct HexCoords : IEquatable<HexCoords>
{
    public int Q { get; }
    public int R { get; }
    public int S => -Q - R;

    public HexCoords(int q, int r)
    {
        Q = q;
        R = r;
    }

    public static HexCoords Zero => new HexCoords(0, 0);

    // 6 directions starting from East/Right-down in pointy-topped axial coords
    private static readonly HexCoords[] Directions = new[]
    {
        new HexCoords(1, 0),   // +q (East-ish)
        new HexCoords(0, 1),   // +r (South-East)
        new HexCoords(-1, 1),  // -q +r (South-West)
        new HexCoords(-1, 0),  // -q (West-ish)
        new HexCoords(0, -1),  // -r (North-West)
        new HexCoords(1, -1)   // +q -r (North-East)
    };

    public HexCoords GetNeighbor(int direction)
    {
        var dir = Directions[direction % 6];
        return new HexCoords(Q + dir.Q, R + dir.R);
    }

    public int DistanceTo(HexCoords other)
    {
        return (Math.Abs(Q - other.Q) + Math.Abs(R - other.R) + Math.Abs(S - other.S)) / 2;
    }

    // Convert hex coordinate to 2D pixel coordinates (Flat-topped or Pointy-topped)
    public Vector2 ToPixel(float size, bool flatTopped)
    {
        if (flatTopped)
        {
            float x = size * (3.0f / 2.0f * Q);
            float y = size * (Mathf.Sqrt(3.0f) / 2.0f * Q + Mathf.Sqrt(3.0f) * R);
            return new Vector2(x, y);
        }
        else
        {
            float x = size * (Mathf.Sqrt(3.0f) * Q + Mathf.Sqrt(3.0f) / 2.0f * R);
            float y = size * (3.0f / 2.0f * R);
            return new Vector2(x, y);
        }
    }

    // Convert 2D pixel coordinates back to fractional hex and round to nearest HexCoords
    public static HexCoords FromPixel(Vector2 pixel, float size, bool flatTopped)
    {
        double q, r;
        if (flatTopped)
        {
            q = (2.0 / 3.0 * pixel.X) / size;
            r = (-1.0 / 3.0 * pixel.X + Math.Sqrt(3.0) / 3.0 * pixel.Y) / size;
        }
        else
        {
            q = (Math.Sqrt(3.0) / 3.0 * pixel.X - 1.0 / 3.0 * pixel.Y) / size;
            r = (2.0 / 3.0 * pixel.Y) / size;
        }
        return Round(q, r);
    }

    public static HexCoords Round(double fracQ, double fracR)
    {
        double fracS = -fracQ - fracR;
        int q = (int)Math.Round(fracQ);
        int r = (int)Math.Round(fracR);
        int s = (int)Math.Round(fracS);

        double qDiff = Math.Abs(q - fracQ);
        double rDiff = Math.Abs(r - fracR);
        double sDiff = Math.Abs(s - fracS);

        if (qDiff > rDiff && qDiff > sDiff)
        {
            q = -r - s;
        }
        else if (rDiff > sDiff)
        {
            r = -q - s;
        }
        return new HexCoords(q, r);
    }

    // Convert from rectangular offset coordinates (Flat-topped, odd-q layout) to Axial
    public static HexCoords FromOffset(int col, int row)
    {
        int q = col;
        int r = row - (col >> 1); // arithmetic shift is floor division for negative numbers
        return new HexCoords(q, r);
    }

    // Convert from Axial back to rectangular offset coordinates
    public void ToOffset(out int col, out int row)
    {
        col = Q;
        row = R + (Q >> 1); // arithmetic shift is floor division for negative numbers
    }

    // Generate list of all coordinates within a local map of radius R
    public static List<HexCoords> GenerateLocalMapCoords(int radius = 3)
    {
        var list = new List<HexCoords>();
        for (int q = -radius; q <= radius; q++)
        {
            int rStart = Math.Max(-radius, -radius - q);
            int rEnd = Math.Min(radius, radius - q);
            for (int r = rStart; r <= rEnd; r++)
            {
                list.Add(new HexCoords(q, r));
            }
        }
        return list;
    }

    public static HexCoords operator +(HexCoords a, HexCoords b) => new HexCoords(a.Q + b.Q, a.R + b.R);
    public static HexCoords operator -(HexCoords a, HexCoords b) => new HexCoords(a.Q - b.Q, a.R - b.R);

    #region Equality & Boilerplate
    public bool Equals(HexCoords other) => Q == other.Q && R == other.R;
    public override bool Equals(object? obj) => obj is HexCoords other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Q, R);
    public static bool operator ==(HexCoords left, HexCoords right) => left.Equals(right);
    public static bool operator !=(HexCoords left, HexCoords right) => !left.Equals(right);
    public override string ToString() => $"({Q},{R})";
    #endregion
}
