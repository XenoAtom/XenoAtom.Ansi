// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Represents a parsed mouse event from an xterm-compatible mouse protocol (e.g. SGR: <c>CSI &lt; ... M/m</c>).
/// </summary>
/// <param name="Action">The mouse action.</param>
/// <param name="X">The 1-based column.</param>
/// <param name="Y">The 1-based row.</param>
/// <param name="Button">The mouse button (when applicable).</param>
/// <param name="WheelDelta">Wheel delta (positive for up, negative for down; 0 otherwise).</param>
/// <param name="Modifiers">Associated modifiers.</param>
public readonly record struct AnsiMouseEvent(
    AnsiMouseAction Action,
    int X,
    int Y,
    AnsiMouseButton Button = AnsiMouseButton.None,
    int WheelDelta = 0,
    AnsiKeyModifiers Modifiers = AnsiKeyModifiers.None);

