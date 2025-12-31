// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tokens;

/// <summary>
/// Represents an SS3 (Single Shift 3) sequence of the form <c>ESC O final</c>.
/// </summary>
/// <remarks>
/// SS3 is commonly used for input sequences in terminals (e.g. arrow keys in application mode and F1–F4).
/// This tokenizer keeps the token syntactic so callers can interpret the final byte as needed.
/// </remarks>
/// <param name="Final">Final byte (typically 0x40–0x7E) identifying the command.</param>
/// <param name="Raw">Optional raw sequence as encountered.</param>
public sealed record Ss3Token(char Final, string? Raw = null) : AnsiToken;

