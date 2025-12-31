// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Represents a parsed keyboard input event.
/// </summary>
/// <param name="Key">The key.</param>
/// <param name="Modifiers">Associated modifiers.</param>
public readonly record struct AnsiKeyEvent(AnsiKey Key, AnsiKeyModifiers Modifiers = AnsiKeyModifiers.None);

