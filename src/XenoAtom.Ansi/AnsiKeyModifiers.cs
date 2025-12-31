// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Modifier keys associated with an input event.
/// </summary>
[Flags]
public enum AnsiKeyModifiers
{
    /// <summary>
    /// No modifiers.
    /// </summary>
    None = 0,

    /// <summary>
    /// Shift key.
    /// </summary>
    Shift = 1 << 0,

    /// <summary>
    /// Alt key.
    /// </summary>
    Alt = 1 << 1,

    /// <summary>
    /// Control key.
    /// </summary>
    Control = 1 << 2,
}
