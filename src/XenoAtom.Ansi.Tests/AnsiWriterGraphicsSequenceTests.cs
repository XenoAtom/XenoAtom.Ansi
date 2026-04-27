// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi.Tokens;

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiWriterGraphicsSequenceTests
{
    [TestMethod]
    public void GenericStringControlWriters_EmitExpectedSequences()
    {
        var output = AnsiRoundTrip.Emit(w =>
        {
            w.WriteOsc(9, "data".AsSpan());
            w.WriteOsc(10, "bell".AsSpan(), AnsiOscTermination.Bell);
            w.WriteDcs("dcs".AsSpan());
            w.WriteSos("sos".AsSpan());
            w.WritePm("pm".AsSpan());
            w.WriteApc("apc".AsSpan());
        });

        Assert.AreEqual("\x1b]9;data\x1b\\\x1b]10;bell\x07\x1bPdcs\x1b\\\x1bXsos\x1b\\\x1b^pm\x1b\\\x1b_apc\x1b\\", output);

        using var tokenizer = new AnsiTokenizer();
        var tokens = tokenizer.Tokenize(output.AsSpan());
        Assert.HasCount(6, tokens);
        Assert.IsInstanceOfType<OscToken>(tokens[0]);
        Assert.IsInstanceOfType<OscToken>(tokens[1]);
        Assert.AreEqual(AnsiStringControlKind.Dcs, ((AnsiStringControlToken)tokens[2]).Kind);
        Assert.AreEqual(AnsiStringControlKind.Sos, ((AnsiStringControlToken)tokens[3]).Kind);
        Assert.AreEqual(AnsiStringControlKind.Pm, ((AnsiStringControlToken)tokens[4]).Kind);
        Assert.AreEqual(AnsiStringControlKind.Apc, ((AnsiStringControlToken)tokens[5]).Kind);
    }

    [TestMethod]
    public void ProtocolHelpers_EmitExpectedSequences()
    {
        var output = AnsiRoundTrip.Emit(w =>
        {
            AnsiKittyGraphicsSequences.WriteCommand(w, "a=T,f=100".AsSpan(), "QUJD".AsSpan());
            AnsiIterm2ImageSequences.WriteFile(w, "inline=1;width=10".AsSpan(), "QUJD".AsSpan());
            AnsiSixelSequences.WriteImage(w, "0;1".AsSpan(), "#0;2;100;0;0??".AsSpan());
        });

        Assert.AreEqual("\x1b_Ga=T,f=100;QUJD\x1b\\\x1b]1337;File=inline=1;width=10:QUJD\x1b\\\x1bP0;1q#0;2;100;0;0??\x1b\\", output);
    }

    [TestMethod]
    public void KittyCommandChunks_AddMoreChunkFlags()
    {
        var output = AnsiRoundTrip.Emit(w =>
            AnsiKittyGraphicsSequences.WriteCommandChunks(w, "a=T".AsSpan(), "q=2".AsSpan(), "abcdef".AsSpan(), maxPayloadChunkChars: 3));

        Assert.AreEqual("\x1b_Ga=T,m=1;abc\x1b\\\x1b_Gq=2,m=0;def\x1b\\", output);
    }

    [TestMethod]
    public void StringControlWriters_RejectUnsafePayloads()
    {
        Assert.Throws<ArgumentException>(() => AnsiRoundTrip.Emit(w => w.WriteDcs("bad\x1b".AsSpan())));
        Assert.Throws<ArgumentException>(() => AnsiRoundTrip.Emit(w => AnsiKittyGraphicsSequences.WriteCommand(w, "a=T;bad".AsSpan(), "QUJD".AsSpan())));
        Assert.Throws<ArgumentException>(() => AnsiRoundTrip.Emit(w => AnsiIterm2ImageSequences.WriteFile(w, "inline=1:bad".AsSpan(), "QUJD".AsSpan())));
    }
}