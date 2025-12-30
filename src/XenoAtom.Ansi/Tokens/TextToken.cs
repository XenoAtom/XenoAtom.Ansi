// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tokens;

/// <summary>
/// Represents plain text with no escape sequences.
/// </summary>
/// <param name="Text">The text content.</param>
public sealed record TextToken(string Text) : AnsiToken;
