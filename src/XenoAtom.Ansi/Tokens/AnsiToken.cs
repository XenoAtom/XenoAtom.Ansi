// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tokens;

/// <summary>
/// Base type for tokens produced by <see cref="XenoAtom.Ansi.AnsiTokenizer"/>.
/// </summary>
/// <remarks>
/// Tokens are a lightweight syntactic representation of ANSI/VT sequences. For deep semantic interpretation,
/// use higher-level helpers such as <see cref="XenoAtom.Ansi.AnsiStyledTextParser"/>.
/// </remarks>
public abstract record AnsiToken;
