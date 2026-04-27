// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tokens;

/// <summary>
/// Represents a terminal string control sequence such as DCS, SOS, PM, or APC.
/// </summary>
/// <param name="Kind">The kind of terminal string control.</param>
/// <param name="Data">The data between the introducer and the ST terminator.</param>
/// <param name="Raw">Optional raw sequence as encountered, including introducer and terminator.</param>
/// <remarks>
/// The tokenizer is syntactic; protocol-specific payloads such as Sixel or Kitty graphics are not decoded here.
/// </remarks>
public sealed record AnsiStringControlToken(AnsiStringControlKind Kind, string Data, string? Raw = null) : AnsiToken;