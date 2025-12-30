// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tokens;

/// <summary>
/// Represents a non-CSI escape sequence of the form <c>ESC intermediates final</c>.
/// </summary>
/// <remarks>
/// In ECMA-48 / ISO/IEC 6429 terminology, this is an "escape sequence" introduced by the C0 control
/// character ESC (U+001B). It is distinct from CSI sequences (which start with <c>ESC [</c>).
///
/// Examples:
/// <list type="bullet">
/// <item><description><c>ESC 7</c> (DECSC, save cursor)</description></item>
/// <item><description><c>ESC 8</c> (DECRC, restore cursor)</description></item>
/// <item><description><c>ESC \\</c> (ST, "String Terminator", used to terminate OSC/DCS/APC/PM/SOS strings)</description></item>
/// </list>
/// This tokenizer keeps the token mostly syntactic so callers can decide how much to interpret.
/// </remarks>
public sealed record EscToken(string Intermediates, char Final, string? Raw = null) : AnsiToken;

