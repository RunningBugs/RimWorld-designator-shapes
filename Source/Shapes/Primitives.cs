﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Merthsoft.DesignatorShapes.Shapes;

public static class Primitives
{
    /// <summary>
    /// Swaps two values.
    /// </summary>
    /// <typeparam name="T">The type of the items we're swapping.</typeparam>
    /// <param name="item1">The first item.</param>
    /// <param name="item2">The second item.</param>
    private static void swap<T>(ref T item1, ref T item2)
    {
        var temp = item1;
        item1 = item2;
        item2 = temp;
    }

    private static IEnumerable<IntVec3> FillCorners(this IEnumerable<IntVec3> shape, IntVec3 center)
    {
        foreach (var cell in shape)
        {
            yield return cell;
            if (cell.x == center.x || cell.z == center.z)
                continue;
            var quadrant = cell.x < center.x && cell.z > center.z ? 0 :
                           cell.x < center.x && cell.z < center.z ? 1 :
                           cell.x > center.x && cell.z < center.z ? 2 :
                           cell.x > center.x && cell.z > center.z ? 3 : throw new Exception($"{cell.x},{cell.y} : {center.x},{center.y}");
            switch (quadrant)
            {
                case 0:
                    if (shape.Contains(cell + IntVec3.NorthEast) && !shape.Contains(cell + IntVec3.East) && !shape.Contains(cell + IntVec3.North))
                        yield return cell + IntVec3.North;
                    break;
                case 1:
                    if (shape.Contains(cell + IntVec3.SouthEast) && !shape.Contains(cell + IntVec3.East) && !shape.Contains(cell + IntVec3.South))
                        yield return cell + IntVec3.South;
                    break;
                case 2:
                    if (shape.Contains(cell + IntVec3.SouthWest) && !shape.Contains(cell + IntVec3.West) && !shape.Contains(cell + IntVec3.South))
                        yield return cell + IntVec3.South;
                    break;
                case 3:
                    if (shape.Contains(cell + IntVec3.NorthWest) && !shape.Contains(cell + IntVec3.West) && !shape.Contains(cell + IntVec3.North))
                        yield return cell + IntVec3.North;
                    break;
            }
        }
    }

    public static IEnumerable<IntVec3> RadialCellsAround(IntVec3 center, float radius) =>
        GenRadial.RadialCellsAround(center, radius, true);

    public static IEnumerable<IntVec3> GenRadialCircle(IntVec3 vert1, float radius, bool filled)
    {
        var innerCells = RadialCellsAround(vert1, radius);

        return filled ? innerCells : RadialCellsAround(vert1, radius + 1).Except(innerCells).FillCorners(vert1);
    }

    public static IEnumerable<IntVec3> Line(IntVec3 vert1, IntVec3 vert2, int thickness, bool fillCorners)
        => Line(vert1.x, vert1.y, vert1.z, vert2.x, vert2.y, vert2.z, thickness, fillCorners);

    public static IEnumerable<IntVec3> Line(int x1, int y1, int z1, int x2, int y2, int z2, int thickness, bool fillCorners)
    {
        var ret = new HashSet<IntVec3> {
            new IntVec3(x1, y1, z1),
            new IntVec3(x2, y2, z2)
        };

        var deltaX = Math.Abs(x1 - x2);
        var deltaZ = Math.Abs(z1 - z2);
        var stepX = x2 < x1 ? 1 : -1;
        var stepZ = z2 < z1 ? 1 : -1;

        var err = deltaX - deltaZ;

        while (true)
        {
            ret.AddRange(PlotPoint(x2, y1, z2, thickness, fillCorners));
            if (x2 == x1 && z2 == z1)
                break;
            var e2 = 2 * err;

            if (e2 > -deltaZ)
            {
                err -= deltaZ;
                x2 += stepX;
            }

            if (x2 == x1 && z2 == z1)
                break;
            if (fillCorners && thickness == 1)
                ret.Add(new IntVec3(x2, y1, z2));

            if (e2 < deltaX)
            {
                err += deltaX;
                z2 += stepZ;
            }
        }

        return ret;
    }

    private static IEnumerable<IntVec3> PlotPoint(int x, int y, int z, int thickness, bool fillCorners)
    {
        if (thickness == 1)
            return new[] { new IntVec3(x, y, z) };
        var negativeReducer = thickness < 0 ? -thickness % 2 == 0 ? 1 : 0 : 0;

        var positiveReducer = thickness > 0 ? thickness % 2 == 0 ? 1 : 0 : 0;
        var step = thickness.Magnitude() / 2;
        var corner1 = new IntVec3(x - (step - negativeReducer), y, z - (step - negativeReducer));
        var corner2 = new IntVec3(x + step - positiveReducer, y, z + step - positiveReducer);
        return Rectangle(corner1.x, corner1.y, corner1.z, corner2.x, corner2.y, corner2.z, true, 0, 1, fillCorners);
    }

    private static IEnumerable<int> Range(int start, int count, bool direction = true)
    {
        if (count < 0)
        {
            count = -count;
            direction = !direction;
        }

        for (var i = 0; i < count; i++)
            yield return start + (direction ? i : -i);
    }

    public static IEnumerable<IntVec3> HorizontalLine(int x1, int x2, int y, int z, int thickness, bool direction)
    {
        if (x1 > x2)
            swap(ref x1, ref x2);
        return Range(x1, x2 - x1 + 1).SelectMany(x => Range(0, thickness, direction).Select(t => new IntVec3(x, y, z + t)));
    }

    public static IEnumerable<IntVec3> VerticalLine(int x, int y, int z1, int z2, int thickness, bool direction)
    {
        if (z1 > z2)
            swap(ref z1, ref z2);
        return Range(z1, z2 - z1 + 1).SelectMany(z => Range(0, thickness, direction).Select(t => new IntVec3(x + t, y, z)));
    }

    public static IEnumerable<IntVec3> Rectangle(int x1, int y1, int z1, int x2, int y2, int z2, bool fill, int rotation, int thickness, bool fillCorners)
    {
        var ret = new HashSet<IntVec3>();

        if (x1 > x2)
            swap(ref x1, ref x2);

        if (z1 > z2)
            swap(ref z1, ref z2);

        if (thickness < 0)
        {
            x1 += thickness + 1;
            z1 += thickness + 1;
            x2 -= thickness + 1;
            z2 -= thickness + 1;

            thickness = -thickness;
        }

        if (rotation == 0)
        {
            if (fill)
                for (var x = x1; x <= x2; x++)
                    ret.AddRange(VerticalLine(x, y1, z1, z2, 1, false));
            else
            {
                ret.AddRange(HorizontalLine(x1, x2, y1, z1, thickness, true));
                ret.AddRange(HorizontalLine(x1, x2, y1, z2, thickness, false));
                ret.AddRange(VerticalLine(x1, y1, z1, z2, thickness, true));
                ret.AddRange(VerticalLine(x2, y1, z1, z2, thickness, false));
            }
            return ret;
        }
        else
        {
            if (x1 > x2)
                swap(ref x1, ref x2);

            if (z1 > z2)
                swap(ref z1, ref z2);

            var hr = (x2 - x1) / 2;
            var kr = (z2 - z1) / 2;

            var A = new IntVec3(x1, y1, z1 + kr);
            var B = new IntVec3(x1 + hr, y1, z1);
            var C = new IntVec3(x2, y2, z1 + kr);
            var D = new IntVec3(x1 + hr, y1, z2);

            ret.AddRange(Line(A, B, thickness, fillCorners));
            ret.AddRange(Line(C, B, thickness, fillCorners));
            ret.AddRange(Line(A, D, thickness, fillCorners));
            ret.AddRange(Line(C, D, thickness, fillCorners));

            return fill ? Fill(ret) : ret;
        }
    }

    public static IEnumerable<IntVec3> Octagon(int sx, int sy, int sz, int tx, int ty, int tz, bool fill, int rotation)
    {
        var ret = new HashSet<IntVec3>();
        return ret;
    }

    public static IEnumerable<IntVec3> Pentagon(int sx, int sy, int sz, int tx, int ty, int tz, bool fill, int rotation, int thickness, bool fillCorners)
    {
        var ret = new HashSet<IntVec3>();

        IntVec3 A, B, C, D, E;
        var width = tx - sx;
        var height = tz - sz;

        var middleX = width / 2 + sx;
        var middleZ = height / 2 + sz;

        var thirdWidth = width / 3;
        var thirdHeight = height / 3;

        switch (rotation)
        {
            case 0:
            default:
                A = new IntVec3(middleX, sy, sz);
                B = new IntVec3(sx, sy, middleZ);
                C = new IntVec3(tx, ty, middleZ);
                D = new IntVec3(sx + thirdWidth, sy, tz);
                E = new IntVec3(tx - thirdWidth, sy, tz);
                break;
            case 1:
                A = new IntVec3(sx, sy, middleZ);
                B = new IntVec3(middleX, sy, sz);
                C = new IntVec3(middleX, ty, tz);
                D = new IntVec3(tx, sy, sz + thirdHeight);
                E = new IntVec3(tx, sy, tz - thirdHeight);
                break;
            case 2:
                A = new IntVec3(middleX, sy, tz);
                B = new IntVec3(sx, sy, middleZ);
                C = new IntVec3(tx, ty, middleZ);
                D = new IntVec3(sx + thirdWidth, sy, sz);
                E = new IntVec3(tx - thirdWidth, sy, sz);
                break;
            case 3:
                A = new IntVec3(tx, sy, middleZ);
                B = new IntVec3(middleX, sy, sz);
                C = new IntVec3(middleX, ty, tz);
                D = new IntVec3(sx, sy, sz + thirdHeight);
                E = new IntVec3(sx, sy, tz - thirdHeight);
                break;

        }

        ret.AddRange(Line(A, B, thickness, fillCorners));
        ret.AddRange(Line(A, C, thickness, fillCorners));
        ret.AddRange(Line(B, D, thickness, fillCorners));
        ret.AddRange(Line(C, E, thickness, fillCorners));
        ret.AddRange(Line(D, E, thickness, fillCorners));

        return fill ? Fill(ret) : ret;
    }

    public static IEnumerable<IntVec3> MidpointHexagon(IntVec3 s, IntVec3 t, bool filled, int rotation, int thickness, bool fillCorners)
    {
        var x1 = s.x;
        var z1 = s.z;
        var x2 = t.x;
        var z2 = t.z;

        if (x2 < x1)
            swap(ref x1, ref x2);
        if (z2 < z1)
            swap(ref z1, ref z2);
        var r = Math.Max(x2 - x1, z2 - z1);
        return RadialHexagon(s.x, s.y, s.z, r, r, filled, rotation, thickness, fillCorners);
    }

    public static IEnumerable<IntVec3> RadialHexagon(int x, int y, int z, int xRadius, int zRadius, bool fill, int rotation, int thickness, bool fillCorners)
        => Hexagon(x - xRadius, y, z - zRadius, x + xRadius, y, z + zRadius, fill, rotation, thickness, fillCorners);

    public static IEnumerable<IntVec3> Hexagon(int sx, int sy, int sz, int tx, int ty, int tz, bool fill, int rotation, int thickness, bool fillCorners)
    {
        if (tx < sx)
            swap(ref sx, ref tx);
        if (tz < sz)
            swap(ref sz, ref tz);
        IntVec3 A, B, C, D, E, F;

        if (rotation == 0)
        {
            var w = tx - sx;
            var h = tz - sz;
            var mz = h / 2 + sz;
            var wt = w / 4;

            A = new IntVec3(sx, sy, mz);
            B = new IntVec3(sx + wt, sy, sz);
            C = new IntVec3(sx + wt, sy, tz);
            D = new IntVec3(tx - wt, sy, sz);
            E = new IntVec3(tx - wt, sy, tz);
            F = new IntVec3(tx, sy, mz);
        }
        else
        {
            var w = tx - sx;
            var h = tz - sz;
            var mx = w / 2 + sx;
            var ht = h / 4;

            A = new IntVec3(mx, sy, sz);
            B = new IntVec3(tx, ty, sz + ht);
            C = new IntVec3(sx, sy, sz + ht);
            D = new IntVec3(tx, ty, tz - ht);
            E = new IntVec3(sx, sy, tz - ht);
            F = new IntVec3(mx, ty, tz);
        }

        var ret = new HashSet<IntVec3>();

        ret.AddRange(Line(A, B, thickness, fillCorners));
        ret.AddRange(Line(B, D, thickness, fillCorners));
        ret.AddRange(Line(F, D, thickness, fillCorners));
        ret.AddRange(Line(A, C, thickness, fillCorners));
        ret.AddRange(Line(C, E, thickness, fillCorners));
        ret.AddRange(Line(F, E, thickness, fillCorners));

        return fill ? Fill(ret) : ret;
    }

    private static void AddRange(this HashSet<IntVec3> vectors, IEnumerable<IntVec3> newVectors)
    {
        foreach (var vec in newVectors)
            vectors.Add(vec);
    }

    /// <summary>
    /// Draws an ellipse to the sprite.
    /// </summary>
    /// <param name="x1"></param>
    /// <param name="z1"></param>
    /// <param name="x2"></param>
    /// <param name="z2"></param>
    /// <param name="fill">True to fill the ellipse.</param>
    public static IEnumerable<IntVec3> Ellipse(
        int x1, int y1, int z1,
        int x2, int y2, int z2,
        bool fill, int thickness, bool fillCorners)
    {
        int dx = x2 - x1;
        int dz = z2 - z1;

        int rx = Math.Abs(dx) / 2;
        int rz = Math.Abs(dz) / 2;

        if (rx == 0 && rz == 0)
        {
            yield return new IntVec3(x1, y1, z1);
            yield break;
        }

        if (rx == 0 || rz == 0)
        {
            foreach (var p in Line(x1, y1, z1, x2, y2, z2, thickness, fillCorners))
                yield return p;
            yield break;
        }

        // Ellipse center is midpoint between anchor and current cursor
        int h = x1 + dx / 2;
        int k = z1 + dz / 2;

        foreach (var p in RadialEllipse(h, y1, k, rx, rz, fill, thickness, fillCorners))
        {
            yield return p;
        }
    }


    /// <summary>
    /// Draws a filled ellipse to the sprite.
    /// </summary>
    /// <remarks>Taken from http://enchantia.com/graphapp/doc/tech/ellipses.html.</remarks>
    /// <param name="x">The center point X coordinate.</param>
    /// <param name="z">The center point Z coordinate.</param>
    /// <param name="xRadius">The x radius.</param>
    /// <param name="zRadius">The z radius.</param>
    /// <param name="fill">True to fill the ellipse.</param>
    public static IEnumerable<IntVec3> RadialEllipse(int x, int y, int z, int xRadius, int zRadius, bool fill, int thickness, bool fillCorners)
    {
        var ret = new HashSet<IntVec3>();

        if (thickness != 1)
        {
            foreach (var i in thickness.Range())
                ret.AddRange(RadialEllipse(x, y, z, xRadius - i, zRadius - i, fill, 1, true));

            return ret;
        }

        var plotX = 0;
        var plotZ = zRadius;

        var xRadiusSquared = xRadius * xRadius;
        var zRadiusSquared = zRadius * zRadius;

        var crit1 = -(xRadiusSquared / 4 + xRadius % 2 + zRadiusSquared);
        var crit2 = -(zRadiusSquared / 4 + zRadius % 2 + xRadiusSquared);
        var crit3 = -(zRadiusSquared / 4 + zRadius % 2);

        var t = -xRadiusSquared * plotZ;
        var dxt = 2 * zRadiusSquared * plotX;
        var dzt = -2 * xRadiusSquared * plotZ;
        var d2xt = 2 * zRadiusSquared;
        var d2zt = 2 * xRadiusSquared;

        while (plotZ >= 0 && plotX <= xRadius)
        {
            circlePlot(x, y, z, ret, plotX, plotZ, fill, thickness, fillCorners);

            if (t + zRadiusSquared * plotX <= crit1 || t + xRadiusSquared * plotZ <= crit3)
                incrementX(ref plotX, ref dxt, ref d2xt, ref t);
            else if (t - xRadiusSquared * plotZ > crit2)
                incrementY(ref plotZ, ref dzt, ref d2zt, ref t);
            else
            {
                incrementX(ref plotX, ref dxt, ref d2xt, ref t);
                if (fillCorners)
                    circlePlot(x, y, z, ret, plotX, plotZ, fill, 1, fillCorners);
                incrementY(ref plotZ, ref dzt, ref d2zt, ref t);
            }
        }

        return ret;

        static void incrementX(ref int x, ref int dxt, ref int d2xt, ref int t)
        {
            x++;
            dxt += d2xt;
            t += dxt;
        }

        static void incrementY(ref int y, ref int dyt, ref int d2yt, ref int t)
        {
            y--;
            dyt += d2yt;
            t += dyt;
        }

        static void circlePlot(int x, int y, int z, HashSet<IntVec3> ret, int plotX, int plotZ, bool fill, int thickness, bool fillCorners)
        {
            var center = new IntVec3(x, y, z);
            ret.AddRange(plotOrLine(center, new IntVec3(x + plotX, 0, z + plotZ), fill, thickness, fillCorners));
            if (plotX != 0 || plotZ != 0)
                ret.AddRange(plotOrLine(center, new IntVec3(x - plotX, 0, z - plotZ), fill, thickness, fillCorners));

            if (plotX != 0 && plotZ != 0)
            {
                ret.AddRange(plotOrLine(center, new IntVec3(x + plotX, 0, z - plotZ), fill, thickness, fillCorners));
                ret.AddRange(plotOrLine(center, new IntVec3(x - plotX, 0, z + plotZ), fill, thickness, fillCorners));
            }
        }

        static IEnumerable<IntVec3> plotOrLine(IntVec3 point1, IntVec3 point2, bool line, int thickness, bool fillCorners)
        => line ? Line(point1, point2, 1, fillCorners)
                : PlotPoint(point2.x, point2.y, point2.z, thickness, fillCorners);
    }

    public static IEnumerable<IntVec3> Circle(IntVec3 s, IntVec3 t, bool filled, int thickness, bool fillCorners)
    {
        var x1 = s.x;
        var z1 = s.z;
        var x2 = t.x;
        var z2 = t.z;

        if (x2 < x1)
            swap(ref x1, ref x2);
        if (z2 < z1)
            swap(ref z1, ref z2);
        var r = Math.Max(x2 - x1, z2 - z1);
        return Circle(s.x, s.y, s.z, r, filled, thickness, fillCorners);
    }

    public static IEnumerable<IntVec3> Circle(IntVec3 center, int r, bool fill, int thickness, bool fillCorners) 
        => Circle(center.x, center.y, center.z, r, fill, thickness, fillCorners);

    /// <summary>
    /// Draws a circle to the sprite.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="r"></param>
    /// <param name="fill">True to fill the circle.</param>
    public static IEnumerable<IntVec3> Circle(int x, int y, int z, int r, bool fill, int thickness, bool fillCorners) 
        => RadialEllipse(x, y, z, r, r, fill, thickness, fillCorners);

    public static IEnumerable<IntVec3> Fill(IEnumerable<IntVec3> outLine)
    {
        var ret = new HashSet<IntVec3>();
        foreach (var lineGroup in outLine.GroupBy(vec => vec.z))
            if (lineGroup.Count() == 1)
                ret.Add(lineGroup.First());
            else
            {
                var sorted = lineGroup.OrderBy(v => v.x);
                var point1 = sorted.First();
                var point2 = sorted.Last();
                ret.AddRange(HorizontalLine(point1.x, point2.x, point1.y, lineGroup.Key, 1, true));
            }

        return ret;
    }
}
