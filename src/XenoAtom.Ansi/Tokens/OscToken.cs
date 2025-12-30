// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tokens;

/// <summary>
/// Represents an OSC (Operating System Command) sequence of the form <c>ESC ] code ; data ST</c> (or BEL-terminated).
/// </summary>
/// <param name="Code">The numeric OSC code.</param>
/// <param name="Data">The raw data portion after the first <c>;</c>.</param>
/// <param name="Raw">Optional raw sequence as encountered.</param>
/// <remarks>
/// OSC is used for terminal "out-of-band" features. A common example is OSC 8 hyperlinks.
/// </remarks>
public sealed record OscToken(int Code, string Data, string? Raw = null) : AnsiToken;
