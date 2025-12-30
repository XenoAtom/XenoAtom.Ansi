// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tokens;

/// <summary>
/// Represents a CSI (Control Sequence Introducer) sequence, typically of the form <c>ESC [ parameters intermediates final</c>.
/// </summary>
/// <param name="Intermediates">Intermediate bytes (0x20–0x2F) captured as a string.</param>
/// <param name="Parameters">Parsed numeric parameters (typically separated by <c>;</c>).</param>
/// <param name="Final">Final byte (0x40–0x7E) identifying the command.</param>
/// <param name="PrivateMarker">Optional private marker (e.g. <c>?</c> for DEC private modes).</param>
/// <param name="Raw">Optional raw sequence as encountered.</param>
/// <remarks>
/// Examples:
/// <list type="bullet">
/// <item><description><c>ESC [ 31 m</c> (SGR red foreground)</description></item>
/// <item><description><c>ESC [ ? 25 l</c> (hide cursor)</description></item>
/// </list>
/// </remarks>
public sealed record CsiToken(string Intermediates, int[] Parameters, char Final, char? PrivateMarker = null, string? Raw = null) : AnsiToken;
