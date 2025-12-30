// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tokens;

/// <summary>
/// Represents a C0 control character (for example LF, CR, TAB, BEL).
/// </summary>
/// <param name="Control">The control character.</param>
public sealed record ControlToken(char Control) : AnsiToken;
