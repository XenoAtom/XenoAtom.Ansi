// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Represents SGR (Select Graphic Rendition) decoration flags.
/// </summary>
/// <remarks>
/// These map to common SGR parameters such as <c>1</c> (bold), <c>4</c> (underline), <c>9</c> (strikethrough), etc.
/// </remarks>
[Flags]
public enum AnsiDecorations
{
    /// <summary>
    /// No decorations.
    /// </summary>
    None = 0,

    /// <summary>SGR 1.</summary>
    Bold = 1 << 0,
    /// <summary>SGR 2.</summary>
    Dim = 1 << 1,
    /// <summary>SGR 3.</summary>
    Italic = 1 << 2,
    /// <summary>SGR 4.</summary>
    Underline = 1 << 3,
    /// <summary>SGR 5 (blink; often not supported).</summary>
    Blink = 1 << 4,
    /// <summary>SGR 7 (reverse video / invert).</summary>
    Invert = 1 << 5,
    /// <summary>SGR 8 (conceal / hidden; often not supported).</summary>
    Hidden = 1 << 6,
    /// <summary>SGR 9 (strikethrough).</summary>
    Strikethrough = 1 << 7,
}
