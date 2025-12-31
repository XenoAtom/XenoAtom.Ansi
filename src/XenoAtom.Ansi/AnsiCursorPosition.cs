// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Represents a 1-based cursor position (row/column).
/// </summary>
/// <param name="Row">The 1-based row.</param>
/// <param name="Column">The 1-based column.</param>
public readonly record struct AnsiCursorPosition(int Row, int Column);

