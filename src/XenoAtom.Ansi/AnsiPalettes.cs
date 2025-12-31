// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi.Helpers;

namespace XenoAtom.Ansi;

/// <summary>
/// Provides palette helpers for converting ANSI palette indices to approximate RGB values.
/// </summary>
/// <remarks>
/// ANSI "basic 16" colors and xterm "256-color" values are indices. Terminals are free to map those indices
/// to different RGB values (themes). This class exposes pragmatic xterm-like defaults that are useful when
/// you need to render ANSI content into a non-terminal surface.
/// </remarks>
public static class AnsiPalettes
{
    private static readonly AnsiColor[] Basic16Colors =
    [
        AnsiColor.Basic16(0),
        AnsiColor.Basic16(1),
        AnsiColor.Basic16(2),
        AnsiColor.Basic16(3),
        AnsiColor.Basic16(4),
        AnsiColor.Basic16(5),
        AnsiColor.Basic16(6),
        AnsiColor.Basic16(7),
        AnsiColor.Basic16(8),
        AnsiColor.Basic16(9),
        AnsiColor.Basic16(10),
        AnsiColor.Basic16(11),
        AnsiColor.Basic16(12),
        AnsiColor.Basic16(13),
        AnsiColor.Basic16(14),
        AnsiColor.Basic16(15),
    ];

    /// <summary>
    /// Gets the 16 palette indices as <see cref="AnsiColor"/> values.
    /// </summary>
    public static ReadOnlySpan<AnsiColor> Basic16 => Basic16Colors;

    /// <summary>
    /// Gets an approximate RGB triple for a given basic-16 palette index, using xterm-like defaults.
    /// </summary>
    /// <param name="index">The palette index in range [0, 15].</param>
    public static (byte R, byte G, byte B) GetBasic16Rgb(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, 15);

        var (r, g, b) = AnsiColorPalette.GetBasic16Rgb(index);
        return (r, g, b);
    }

    /// <summary>
    /// Gets an RGB triple for a given xterm 256-color palette index.
    /// </summary>
    /// <param name="index">The palette index in range [0, 255].</param>
    public static (byte R, byte G, byte B) GetXterm256Rgb(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, 255);

        var (r, g, b) = AnsiColorPalette.GetXterm256Rgb(index);
        return (r, g, b);
    }
}

