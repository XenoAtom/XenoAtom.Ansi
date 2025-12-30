// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Provides named ANSI colors for the "basic 16" color palette.
/// </summary>
/// <remarks>
/// These values represent palette indices (0–15) used by SGR foreground/background codes
/// (e.g. <c>ESC [ 31 m</c> for red foreground, <c>ESC [ 91 m</c> for bright red foreground).
///
/// The actual RGB values displayed for these indices are terminal/theme dependent.
/// If you need RGB approximations for rendering, use <see cref="AnsiPalettes"/>.
/// </remarks>
public static class AnsiColors
{
    /// <summary>
    /// The terminal default color (SGR 39/49).
    /// </summary>
    public static AnsiColor Default => AnsiColor.Default;

    /// <summary>Basic color index 0 (black).</summary>
    public static AnsiColor Black => AnsiColor.Basic16(0);

    /// <summary>Basic color index 1 (red).</summary>
    public static AnsiColor Red => AnsiColor.Basic16(1);

    /// <summary>Basic color index 2 (green).</summary>
    public static AnsiColor Green => AnsiColor.Basic16(2);

    /// <summary>Basic color index 3 (yellow).</summary>
    public static AnsiColor Yellow => AnsiColor.Basic16(3);

    /// <summary>Basic color index 4 (blue).</summary>
    public static AnsiColor Blue => AnsiColor.Basic16(4);

    /// <summary>Basic color index 5 (magenta).</summary>
    public static AnsiColor Magenta => AnsiColor.Basic16(5);

    /// <summary>Basic color index 6 (cyan).</summary>
    public static AnsiColor Cyan => AnsiColor.Basic16(6);

    /// <summary>Basic color index 7 (white).</summary>
    public static AnsiColor White => AnsiColor.Basic16(7);

    /// <summary>Basic color index 8 (bright black / "gray").</summary>
    public static AnsiColor BrightBlack => AnsiColor.Basic16(8);

    /// <summary>Basic color index 9 (bright red).</summary>
    public static AnsiColor BrightRed => AnsiColor.Basic16(9);

    /// <summary>Basic color index 10 (bright green).</summary>
    public static AnsiColor BrightGreen => AnsiColor.Basic16(10);

    /// <summary>Basic color index 11 (bright yellow).</summary>
    public static AnsiColor BrightYellow => AnsiColor.Basic16(11);

    /// <summary>Basic color index 12 (bright blue).</summary>
    public static AnsiColor BrightBlue => AnsiColor.Basic16(12);

    /// <summary>Basic color index 13 (bright magenta).</summary>
    public static AnsiColor BrightMagenta => AnsiColor.Basic16(13);

    /// <summary>Basic color index 14 (bright cyan).</summary>
    public static AnsiColor BrightCyan => AnsiColor.Basic16(14);

    /// <summary>Basic color index 15 (bright white).</summary>
    public static AnsiColor BrightWhite => AnsiColor.Basic16(15);
}

