// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Helpers;

internal static class AnsiColorPalette
{
    /// <summary>
    /// Gets an approximate RGB value for a given 16-color index, using xterm-like defaults.
    /// </summary>
    /// <remarks>
    /// The actual colors displayed by a terminal for the "basic 16" palette are theme-dependent.
    /// This mapping is provided as a pragmatic default for renderers that need an RGB fallback.
    /// </remarks>
    public static (byte r, byte g, byte b) GetBasic16Rgb(int index) => Basic16ToRgb(index);

    /// <summary>
    /// Gets an RGB value for a given xterm 256-color palette index.
    /// </summary>
    public static (byte r, byte g, byte b) GetXterm256Rgb(int index) => Xterm256ToRgb(index);

    public static AnsiColor ToXterm256(byte r, byte g, byte b)
    {
        // Map RGB to xterm 256-color cube (16..231) or grayscale ramp (232..255).
        // This is a pragmatic approximation; terminals differ slightly.
        var bestIndex = 0;
        var bestDistance = int.MaxValue;

        // Color cube.
        var rc = ToCubeIndex(r);
        var gc = ToCubeIndex(g);
        var bc = ToCubeIndex(b);
        var cubeIndex = 16 + (36 * rc) + (6 * gc) + bc;
        var (cr, cg, cb) = ColorCubeToRgb(rc, gc, bc);
        bestIndex = cubeIndex;
        bestDistance = DistSq(r, g, b, cr, cg, cb);

        // Grayscale ramp.
        var grayIndex = ToGrayIndex(r, g, b);
        var gray = (byte)(8 + (10 * grayIndex));
        var grayDistance = DistSq(r, g, b, gray, gray, gray);
        if (grayDistance < bestDistance)
        {
            bestIndex = 232 + grayIndex;
            bestDistance = grayDistance;
        }

        _ = bestDistance;
        return AnsiColor.Indexed256(bestIndex);
    }

    public static AnsiColor ToBasic16(int index)
    {
        if (index < 16)
        {
            return AnsiColor.Basic16(index);
        }

        var rgb = Xterm256ToRgb(index);
        return ToBasic16(rgb.r, rgb.g, rgb.b);
    }

    public static AnsiColor ToBasic16(byte r, byte g, byte b)
    {
        var bestIndex = 0;
        var bestDistance = int.MaxValue;

        for (var i = 0; i < 16; i++)
        {
            var (br, bg, bb) = Basic16ToRgb(i);
            var d = DistSq(r, g, b, br, bg, bb);
            if (d < bestDistance)
            {
                bestDistance = d;
                bestIndex = i;
            }
        }

        return AnsiColor.Basic16(bestIndex);
    }

    private static int ToCubeIndex(byte v)
    {
        // 0..255 -> 0..5
        if (v < 48) return 0;
        if (v < 114) return 1;
        return (v - 35) / 40;
    }

    private static int ToGrayIndex(byte r, byte g, byte b)
    {
        var gray = (r + g + b) / 3;
        if (gray < 8) return 0;
        if (gray > 238) return 23;
        return (gray - 8) / 10;
    }

    private static (byte r, byte g, byte b) ColorCubeToRgb(int rc, int gc, int bc)
    {
        static byte to(byte v) => v == 0 ? (byte)0 : (byte)(55 + (40 * v));
        return (to((byte)rc), to((byte)gc), to((byte)bc));
    }

    private static (byte r, byte g, byte b) Basic16ToRgb(int index)
    {
        // xterm-ish defaults (not exact across terminals).
        return index switch
        {
            0 => (0, 0, 0),
            1 => (205, 0, 0),
            2 => (0, 205, 0),
            3 => (205, 205, 0),
            4 => (0, 0, 238),
            5 => (205, 0, 205),
            6 => (0, 205, 205),
            7 => (229, 229, 229),
            8 => (127, 127, 127),
            9 => (255, 0, 0),
            10 => (0, 255, 0),
            11 => (255, 255, 0),
            12 => (92, 92, 255),
            13 => (255, 0, 255),
            14 => (0, 255, 255),
            15 => (255, 255, 255),
            _ => (0, 0, 0),
        };
    }

    private static (byte r, byte g, byte b) Xterm256ToRgb(int index)
    {
        if (index < 16)
        {
            return Basic16ToRgb(index);
        }

        if (index is >= 16 and <= 231)
        {
            var i = index - 16;
            var rc = i / 36;
            var gc = (i % 36) / 6;
            var bc = i % 6;
            return ColorCubeToRgb(rc, gc, bc);
        }

        if (index is >= 232 and <= 255)
        {
            var gray = (byte)(8 + (index - 232) * 10);
            return (gray, gray, gray);
        }

        return (0, 0, 0);
    }

    private static int DistSq(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
    {
        var dr = r1 - r2;
        var dg = g1 - g2;
        var db = b1 - b2;
        return (dr * dr) + (dg * dg) + (db * db);
    }
}
