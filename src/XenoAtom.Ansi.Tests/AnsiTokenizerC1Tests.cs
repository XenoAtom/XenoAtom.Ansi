// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi.Tokens;

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiTokenizerC1Tests
{
    [TestMethod]
    public void Tokenize_C1Csi_Sgr_DecodesToSgrToken()
    {
        using var tok = new AnsiTokenizer();
        var tokens = tok.Tokenize($"a\u009b31mb".AsSpan(), isFinalChunk: true);

        Assert.HasCount(3, tokens);
        Assert.IsInstanceOfType<TextToken>(tokens[0]);
        Assert.IsInstanceOfType<SgrToken>(tokens[1]);
        Assert.IsInstanceOfType<TextToken>(tokens[2]);
    }

    [TestMethod]
    public void Tokenize_C1Osc_TerminatesWithC1St()
    {
        // OSC 0 ; title ST
        var input = "\u009d0;title\u009cX";
        using var tok = new AnsiTokenizer(new AnsiTokenizerOptions { DecodeSgr = false });
        var tokens = tok.Tokenize(input.AsSpan(), isFinalChunk: true);

        Assert.HasCount(2, tokens);
        var osc = (OscToken)tokens[0];
        Assert.AreEqual(0, osc.Code);
        Assert.AreEqual("title", osc.Data);
        Assert.AreEqual("X", ((TextToken)tokens[1]).Text);
    }
}

