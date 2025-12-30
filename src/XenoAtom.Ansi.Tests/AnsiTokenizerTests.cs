// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi.Tokens;

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiTokenizerTests
{
    [TestMethod]
    public void Tokenize_PlainText_FastPath()
    {
        using var tokenizer = new AnsiTokenizer();
        var tokens = tokenizer.Tokenize("abc".AsSpan());
        Assert.HasCount(1, tokens);
        Assert.IsInstanceOfType<TextToken>(tokens[0]);
        Assert.AreEqual("abc", ((TextToken)tokens[0]).Text);
    }

    [TestMethod]
    public void Tokenize_SgrSequence_DecodesOperations()
    {
        using var tokenizer = new AnsiTokenizer();
        var input = "a\x1b[31mb";
        var tokens = tokenizer.Tokenize(input.AsSpan());
        Assert.HasCount(3, tokens);

        Assert.AreEqual("a", ((TextToken)tokens[0]).Text);
        var sgr = (SgrToken)tokens[1];
        Assert.AreEqual("\x1b[31m", sgr.Raw);
        Assert.HasCount(1, sgr.Operations);
        Assert.AreEqual(AnsiSgrOp.SetForeground(AnsiColor.Basic16(1)), sgr.Operations[0]);
        Assert.AreEqual("b", ((TextToken)tokens[2]).Text);
    }

    [TestMethod]
    public void Tokenize_EscSequence_ProducesEscToken()
    {
        using var tokenizer = new AnsiTokenizer();
        var tokens = tokenizer.Tokenize("\u001b7".AsSpan());
        Assert.HasCount(1, tokens);
        var esc = (EscToken)tokens[0];
        Assert.AreEqual('7', esc.Final);
        Assert.AreEqual("\u001b7", esc.Raw);
    }

    [TestMethod]
    public void Tokenize_IncompleteEscape_EmitsUnknownTokenWhenFinal()
    {
        using var tokenizer = new AnsiTokenizer();
        var tokens = tokenizer.Tokenize("\x1b[".AsSpan(), isFinalChunk: true);
        Assert.HasCount(1, tokens);
        var unk = (UnknownEscapeToken)tokens[0];
        Assert.AreEqual("\x1b[", unk.Raw);
    }

    [TestMethod]
    public void Tokenize_Osc8_ParsesCodeAndData()
    {
        using var tokenizer = new AnsiTokenizer();
        var input = "\x1b]8;;https://example.com\x1b\\link\x1b]8;;\x1b\\";
        var tokens = tokenizer.Tokenize(input.AsSpan());

        Assert.HasCount(3, tokens);
        var osc1 = (OscToken)tokens[0];
        Assert.AreEqual(8, osc1.Code);
        Assert.AreEqual(";https://example.com", osc1.Data);
        Assert.AreEqual("link", ((TextToken)tokens[1]).Text);
        var osc2 = (OscToken)tokens[2];
        Assert.AreEqual(8, osc2.Code);
        Assert.AreEqual(";", osc2.Data);
    }
}
