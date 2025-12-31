// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Common keyboard keys as represented by terminal input sequences.
/// </summary>
public enum AnsiKey
{
    /// <summary>
    /// Unknown or unsupported key.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Up arrow.
    /// </summary>
    Up,

    /// <summary>
    /// Down arrow.
    /// </summary>
    Down,

    /// <summary>
    /// Left arrow.
    /// </summary>
    Left,

    /// <summary>
    /// Right arrow.
    /// </summary>
    Right,

    /// <summary>
    /// Home.
    /// </summary>
    Home,

    /// <summary>
    /// End.
    /// </summary>
    End,

    /// <summary>
    /// Insert.
    /// </summary>
    Insert,

    /// <summary>
    /// Delete.
    /// </summary>
    Delete,

    /// <summary>
    /// Page up.
    /// </summary>
    PageUp,

    /// <summary>
    /// Page down.
    /// </summary>
    PageDown,

    /// <summary>
    /// Backspace (commonly DEL / 0x7F on terminal input).
    /// </summary>
    Backspace,

    /// <summary>
    /// Tab.
    /// </summary>
    Tab,

    /// <summary>
    /// Back tab (commonly Shift+Tab).
    /// </summary>
    BackTab,

    /// <summary>
    /// Enter.
    /// </summary>
    Enter,

    /// <summary>
    /// Escape.
    /// </summary>
    Escape,

    /// <summary>
    /// Function key F1.
    /// </summary>
    F1,

    /// <summary>
    /// Function key F2.
    /// </summary>
    F2,

    /// <summary>
    /// Function key F3.
    /// </summary>
    F3,

    /// <summary>
    /// Function key F4.
    /// </summary>
    F4,

    /// <summary>
    /// Function key F5.
    /// </summary>
    F5,

    /// <summary>
    /// Function key F6.
    /// </summary>
    F6,

    /// <summary>
    /// Function key F7.
    /// </summary>
    F7,

    /// <summary>
    /// Function key F8.
    /// </summary>
    F8,

    /// <summary>
    /// Function key F9.
    /// </summary>
    F9,

    /// <summary>
    /// Function key F10.
    /// </summary>
    F10,

    /// <summary>
    /// Function key F11.
    /// </summary>
    F11,

    /// <summary>
    /// Function key F12.
    /// </summary>
    F12,
}
