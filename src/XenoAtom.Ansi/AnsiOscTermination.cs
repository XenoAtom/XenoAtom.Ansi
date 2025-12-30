// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Specifies how an OSC (Operating System Command) string is terminated when emitting.
/// </summary>
/// <remarks>
/// Many terminals accept either:
/// <list type="bullet">
/// <item><description>BEL (U+0007, <c>\x07</c>)</description></item>
/// <item><description>ST (String Terminator) which is the two-character sequence <c>ESC \\</c></description></item>
/// </list>
/// </remarks>
public enum AnsiOscTermination
{
    /// <summary>
    /// Terminates OSC with BEL (U+0007).
    /// </summary>
    Bell = 0,

    /// <summary>
    /// Terminates OSC with ST (<c>ESC \\</c>).
    /// </summary>
    StringTerminator = 1,
}
