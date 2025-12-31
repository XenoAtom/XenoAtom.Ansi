// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Defines cursor styles for DECSCUSR (<c>ESC [ n SP q</c>).
/// </summary>
/// <remarks>
/// These values are commonly supported by xterm-compatible terminals and Windows Terminal.
/// </remarks>
public enum AnsiCursorStyle
{
    /// <summary>
    /// Default cursor style.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Blinking block cursor.
    /// </summary>
    BlinkingBlock = 1,

    /// <summary>
    /// Steady block cursor.
    /// </summary>
    SteadyBlock = 2,

    /// <summary>
    /// Blinking underline cursor.
    /// </summary>
    BlinkingUnderline = 3,

    /// <summary>
    /// Steady underline cursor.
    /// </summary>
    SteadyUnderline = 4,

    /// <summary>
    /// Blinking bar cursor.
    /// </summary>
    BlinkingBar = 5,

    /// <summary>
    /// Steady bar cursor.
    /// </summary>
    SteadyBar = 6,
}

