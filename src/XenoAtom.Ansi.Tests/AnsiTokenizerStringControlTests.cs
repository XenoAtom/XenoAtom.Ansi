// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi.Tokens;

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiTokenizerStringControlTests
{
    [TestMethod]
    public void Tokenize_StringControls_ParsesKindDataAndRaw()
    {
        using var tokenizer = new AnsiTokenizer();
        var tokens = tokenizer.Tokenize("\x1bP1;2qabc\x1b\\\x1bXsos\x1b\\\x1b^pm\x1b\\\x1b_Gi=7;OK\x1b\\".AsSpan());

        Assert.HasCount(4, tokens);

        var dcs = (AnsiStringControlToken)tokens[0];
        Assert.AreEqual(AnsiStringControlKind.Dcs, dcs.Kind);
        Assert.AreEqual("1;2qabc", dcs.Data);
        Assert.AreEqual("\x1bP1;2qabc\x1b\\", dcs.Raw);

        var sos = (AnsiStringControlToken)tokens[1];
        Assert.AreEqual(AnsiStringControlKind.Sos, sos.Kind);
        Assert.AreEqual("sos", sos.Data);

        var pm = (AnsiStringControlToken)tokens[2];
        Assert.AreEqual(AnsiStringControlKind.Pm, pm.Kind);
        Assert.AreEqual("pm", pm.Data);

        var apc = (AnsiStringControlToken)tokens[3];
        Assert.AreEqual(AnsiStringControlKind.Apc, apc.Kind);
        Assert.AreEqual("Gi=7;OK", apc.Data);
    }

    [TestMethod]
    public void Tokenize_C1StringControls_ParsesKindAndData()
    {
        using var tokenizer = new AnsiTokenizer();
        var tokens = tokenizer.Tokenize("\u0090dcs\u009c\u0098sos\u009c\u009epm\u009c\u009fapc\u009c".AsSpan());

        Assert.HasCount(4, tokens);
        Assert.AreEqual(AnsiStringControlKind.Dcs, ((AnsiStringControlToken)tokens[0]).Kind);
        Assert.AreEqual("dcs", ((AnsiStringControlToken)tokens[0]).Data);
        Assert.AreEqual(AnsiStringControlKind.Sos, ((AnsiStringControlToken)tokens[1]).Kind);
        Assert.AreEqual("sos", ((AnsiStringControlToken)tokens[1]).Data);
        Assert.AreEqual(AnsiStringControlKind.Pm, ((AnsiStringControlToken)tokens[2]).Kind);
        Assert.AreEqual("pm", ((AnsiStringControlToken)tokens[2]).Data);
        Assert.AreEqual(AnsiStringControlKind.Apc, ((AnsiStringControlToken)tokens[3]).Kind);
        Assert.AreEqual("apc", ((AnsiStringControlToken)tokens[3]).Data);
    }

    [TestMethod]
    public void Tokenize_StringControlSplitAcrossChunks_ParsesWhenTerminated()
    {
        using var tokenizer = new AnsiTokenizer();
        var first = tokenizer.Tokenize("before\x1b_Gi=7;".AsSpan(), isFinalChunk: false);
        Assert.HasCount(1, first);
        Assert.AreEqual("before", ((TextToken)first[0]).Text);

        var second = tokenizer.Tokenize("OK\x1b\\after".AsSpan(), isFinalChunk: true);
        Assert.HasCount(2, second);

        var apc = (AnsiStringControlToken)second[0];
        Assert.AreEqual(AnsiStringControlKind.Apc, apc.Kind);
        Assert.AreEqual("Gi=7;OK", apc.Data);
        Assert.AreEqual("after", ((TextToken)second[1]).Text);
    }

    [TestMethod]
    public void Tokenize_UnterminatedStringControlWhenFinal_EmitsUnknownEscapeToken()
    {
        using var tokenizer = new AnsiTokenizer();

        var tokens = tokenizer.Tokenize("\x1b_Gi=7;OK".AsSpan(), isFinalChunk: true);

        Assert.HasCount(1, tokens);
        Assert.AreEqual("\x1b_Gi=7;OK", ((UnknownEscapeToken)tokens[0]).Raw);
    }
    [TestMethod]
    public void Tokenize_OversizedStringControl_EmitsUnknownEscapeToken()
    {
        var options = AnsiTokenizerOptions.Default with { MaxEscapeSequenceLength = 3 };
        using var tokenizer = new AnsiTokenizer(options);

        var tokens = tokenizer.Tokenize("\x1bPab".AsSpan(), isFinalChunk: true);

        Assert.HasCount(1, tokens);
        var unknown = (UnknownEscapeToken)tokens[0];
        Assert.AreEqual("\x1bPab", unknown.Raw);
    }

    [TestMethod]
    public void KittyReplyParser_ParsesApcReplyToken()
    {
        using var tokenizer = new AnsiTokenizer();
        var token = (AnsiStringControlToken)tokenizer.Tokenize("\x1b_Gi=31,p=4;OK\x1b\\".AsSpan()).Single();

        Assert.IsTrue(AnsiKittyGraphicsSequences.TryParseReply(token, out var reply));
        Assert.AreEqual(31, reply.ImageId);
        Assert.AreEqual(4, reply.PlacementId);
        Assert.AreEqual("OK", reply.Status);
        Assert.AreEqual("i=31,p=4", reply.Parameters);
        Assert.IsNull(reply.Message);
    }
}