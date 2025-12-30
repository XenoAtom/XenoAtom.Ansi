// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tokens;

/// <summary>
/// Represents an escape sequence that could not be parsed (malformed or intentionally skipped).
/// </summary>
/// <param name="Raw">The raw buffered data.</param>
public sealed record UnknownEscapeToken(string Raw) : AnsiToken;
