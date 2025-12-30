// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Represents a contiguous run of text associated with a style and optional hyperlink.
/// </summary>
/// <param name="Text">The run text.</param>
/// <param name="Style">The style active for this run.</param>
/// <param name="Hyperlink">Optional active hyperlink for this run.</param>
public sealed record AnsiStyledRun(string Text, AnsiStyle Style, AnsiHyperlink? Hyperlink);
