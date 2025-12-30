// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Identifies which kind of ANSI color value is represented by an <see cref="AnsiColor"/>.
/// </summary>
public enum AnsiColorKind : byte
{
    /// <summary>
    /// The terminal default color.
    /// </summary>
    Default = 0,

    /// <summary>
    /// One of the 16 basic palette indices (0–15).
    /// </summary>
    Basic16 = 1,

    /// <summary>
    /// One of the 256-color xterm palette indices (0–255).
    /// </summary>
    Indexed256 = 2,

    /// <summary>
    /// A 24-bit RGB color (truecolor).
    /// </summary>
    Rgb = 3,
}