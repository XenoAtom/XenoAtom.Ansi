// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi.Helpers;

namespace XenoAtom.Ansi;

/// <summary>
/// Represents an ANSI color value that can be encoded/decoded via SGR parameters.
/// </summary>
/// <remarks>
/// This type models the common ANSI/VT color forms used by modern terminals:
/// <list type="bullet">
/// <item><description>Default foreground/background (SGR 39/49)</description></item>
/// <item><description>Basic 16-color palette indices (SGR 30–37/90–97 and 40–47/100–107)</description></item>
/// <item><description>256-color indexed palette (SGR 38;5;n / 48;5;n)</description></item>
/// <item><description>Truecolor (24-bit RGB) (SGR 38;2;r;g;b / 48;2;r;g;b)</description></item>
/// </list>
/// </remarks>
public readonly record struct AnsiColor
{
    /// <summary>
    /// The terminal default color.
    /// </summary>
    public static readonly AnsiColor Default = new(AnsiColorKind.Default, 0, 0, 0, 0);

    private AnsiColor(AnsiColorKind kind, byte index, byte r, byte g, byte b)
    {
        Kind = kind;
        Index = index;
        R = r;
        G = g;
        B = b;
    }

    /// <summary>
    /// Gets the kind of this color value.
    /// </summary>
    public AnsiColorKind Kind { get; }

    /// <summary>
    /// Gets the palette index for <see cref="AnsiColorKind.Basic16"/> or <see cref="AnsiColorKind.Indexed256"/>.
    /// </summary>
    public byte Index { get; }

    /// <summary>
    /// Gets the red component for <see cref="AnsiColorKind.Rgb"/>.
    /// </summary>
    public byte R { get; }

    /// <summary>
    /// Gets the green component for <see cref="AnsiColorKind.Rgb"/>.
    /// </summary>
    public byte G { get; }

    /// <summary>
    /// Gets the blue component for <see cref="AnsiColorKind.Rgb"/>.
    /// </summary>
    public byte B { get; }

    /// <summary>
    /// Creates a basic 16-color palette value.
    /// </summary>
    /// <param name="index">The palette index in range [0, 15].</param>
    public static AnsiColor Basic16(int index)
    {
        if ((uint)index > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Basic16 index must be in range [0, 15].");
        }

        return new AnsiColor(AnsiColorKind.Basic16, (byte)index, 0, 0, 0);
    }

    /// <summary>
    /// Creates a 256-color palette value.
    /// </summary>
    /// <param name="index">The palette index in range [0, 255].</param>
    public static AnsiColor Indexed256(int index)
    {
        if ((uint)index > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Indexed256 index must be in range [0, 255].");
        }

        return new AnsiColor(AnsiColorKind.Indexed256, (byte)index, 0, 0, 0);
    }

    /// <summary>
    /// Creates a truecolor RGB value.
    /// </summary>
    public static AnsiColor Rgb(byte r, byte g, byte b) => new(AnsiColorKind.Rgb, 0, r, g, b);

    /// <summary>
    /// Attempts to downgrade this color to a maximum supported <see cref="AnsiColorLevel"/>.
    /// </summary>
    /// <param name="maxLevel">The maximum supported level.</param>
    /// <param name="downgraded">The resulting color value.</param>
    /// <returns><see langword="true"/> if the color could be represented at the requested level.</returns>
    /// <remarks>
    /// For example, an RGB color can be approximated as an xterm 256-color index, which can then be approximated
    /// as a basic 16-color index.
    /// </remarks>
    public bool TryDowngrade(AnsiColorLevel maxLevel, out AnsiColor downgraded)
    {
        if (Kind == AnsiColorKind.Default)
        {
            downgraded = this;
            return true;
        }

        switch (maxLevel)
        {
            case AnsiColorLevel.None:
                downgraded = Default;
                return true;
            case AnsiColorLevel.Colors16:
                return TryDowngradeTo16(out downgraded);
            case AnsiColorLevel.Colors256:
                return TryDowngradeTo256(out downgraded);
            case AnsiColorLevel.TrueColor:
                downgraded = this;
                return true;
            default:
                downgraded = Default;
                return false;
        }
    }

    private bool TryDowngradeTo256(out AnsiColor downgraded)
    {
        if (Kind == AnsiColorKind.Basic16 || Kind == AnsiColorKind.Indexed256)
        {
            downgraded = this;
            return true;
        }

        if (Kind != AnsiColorKind.Rgb)
        {
            downgraded = Default;
            return false;
        }

        downgraded = AnsiColorPalette.ToXterm256(R, G, B);
        return true;
    }

    private bool TryDowngradeTo16(out AnsiColor downgraded)
    {
        if (Kind == AnsiColorKind.Basic16)
        {
            downgraded = this;
            return true;
        }

        AnsiColor rgbOr256 = this;
        if (!TryDowngrade(AnsiColorLevel.Colors256, out rgbOr256))
        {
            downgraded = Default;
            return false;
        }

        if (rgbOr256.Kind == AnsiColorKind.Basic16)
        {
            downgraded = rgbOr256;
            return true;
        }

        if (rgbOr256.Kind == AnsiColorKind.Indexed256)
        {
            downgraded = AnsiColorPalette.ToBasic16(rgbOr256.Index);
            return true;
        }

        downgraded = Default;
        return false;
    }
}
