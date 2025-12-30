// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi.Tokens;

namespace XenoAtom.Ansi.Tests;

internal static class AnsiRoundTrip
{
    public static string Emit(Action<AnsiWriter> emit, AnsiCapabilities? capabilities = null)
    {
        using var builder = new AnsiBuilder();
        var writer = new AnsiWriter(builder, capabilities ?? AnsiCapabilities.Default);
        emit(writer);
        return builder.ToString();
    }

    public static List<AnsiToken> EmitAndTokenize(Action<AnsiWriter> emit, AnsiCapabilities? capabilities = null)
    {
        var s = Emit(emit, capabilities);
        using var tokenizer = new AnsiTokenizer();
        return tokenizer.Tokenize(s.AsSpan(), isFinalChunk: true);
    }
}

