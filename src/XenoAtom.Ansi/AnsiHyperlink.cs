// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Represents a hyperlink extracted from OSC 8 sequences.
/// </summary>
/// <param name="Uri">The hyperlink target URI.</param>
/// <param name="Id">Optional hyperlink id (xterm convention: <c>id=...</c>).</param>
public readonly record struct AnsiHyperlink(string Uri, string? Id = null);
