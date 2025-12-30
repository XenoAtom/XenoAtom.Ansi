// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tokens;

/// <summary>
/// Represents a decoded SGR (Select Graphic Rendition) CSI sequence (<c>... m</c>).
/// </summary>
/// <param name="Operations">Decoded operations.</param>
/// <param name="Raw">Optional raw sequence as encountered.</param>
public sealed record SgrToken(AnsiSgrOp[] Operations, string? Raw = null) : AnsiToken;
