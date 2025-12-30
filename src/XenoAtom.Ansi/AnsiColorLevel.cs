// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Represents the color feature level supported by the output target.
/// </summary>
public enum AnsiColorLevel
{
    /// <summary>
    /// No color support.
    /// </summary>
    None = 0,

    /// <summary>
    /// The 16-color palette (8 normal + 8 bright).
    /// </summary>
    Colors16 = 1,

    /// <summary>
    /// The xterm 256-color indexed palette.
    /// </summary>
    Colors256 = 2,

    /// <summary>
    /// Truecolor (24-bit RGB) via SGR <c>38;2;r;g;b</c> / <c>48;2;r;g;b</c>.
    /// </summary>
    TrueColor = 3,
}
