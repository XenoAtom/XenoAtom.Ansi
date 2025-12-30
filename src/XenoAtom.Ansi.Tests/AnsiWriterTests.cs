// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi.Tokens;

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiWriterTests
{
    [TestMethod]
    public void Write_Text_ParsesAsTextToken()
    {
        var output = AnsiRoundTrip.Emit(w => w.Write("hello"));
        Assert.AreEqual("hello", output);

        var tokens = AnsiRoundTrip.EmitAndTokenize(w => w.Write("hello"));
        Assert.HasCount(1, tokens);
        Assert.AreEqual("hello", ((TextToken)tokens[0]).Text);
    }

    [TestMethod]
    public void FluentSyntax_CanChain()
    {
        var output = AnsiRoundTrip.Emit(w =>
            w.Foreground(AnsiColors.Red)
                .Decorate(AnsiDecorations.Bold)
                .Write("X")
                .ResetStyle());

        Assert.AreEqual("\x1b[31m\x1b[1mX\x1b[0m", output);

        using var tokenizer = new AnsiTokenizer();
        var tokens = tokenizer.Tokenize(output.AsSpan());
        Assert.HasCount(4, tokens);
        Assert.IsInstanceOfType<SgrToken>(tokens[0]);
        Assert.IsInstanceOfType<SgrToken>(tokens[1]);
        Assert.IsInstanceOfType<TextToken>(tokens[2]);
        Assert.IsInstanceOfType<SgrToken>(tokens[3]);
    }

    [TestMethod]
    public void Reset_EmitsSgr0_AndParsesAsSgr()
    {
        var output = AnsiRoundTrip.Emit(w => w.Reset());
        Assert.AreEqual("\x1b[0m", output);

        using var tokenizer = new AnsiTokenizer();
        var tokens = tokenizer.Tokenize(output.AsSpan());
        Assert.HasCount(1, tokens);
        var sgr = (SgrToken)tokens[0];
        Assert.AreEqual("\x1b[0m", sgr.Raw);
        Assert.HasCount(1, sgr.Operations);
        Assert.AreEqual(AnsiSgrOp.Reset(), sgr.Operations[0]);
    }

    [TestMethod]
    public void Foreground_Basic16_ParsesAsSgr()
    {
        var output = AnsiRoundTrip.Emit(w => w.Foreground(AnsiColor.Basic16(1)));
        Assert.AreEqual("\x1b[31m", output);

        var tokens = AnsiRoundTrip.EmitAndTokenize(w => w.Foreground(AnsiColor.Basic16(1)));
        Assert.HasCount(1, tokens);
        var sgr = (SgrToken)tokens[0];
        Assert.AreEqual(AnsiSgrOp.SetForeground(AnsiColor.Basic16(1)), sgr.Operations.Single());
    }

    [TestMethod]
    public void Foreground_Indexed256_ParsesAsSgr()
    {
        var output = AnsiRoundTrip.Emit(w => w.Foreground(AnsiColor.Indexed256(123)));
        Assert.AreEqual("\x1b[38;5;123m", output);

        var tokens = AnsiRoundTrip.EmitAndTokenize(w => w.Foreground(AnsiColor.Indexed256(123)));
        var sgr = (SgrToken)tokens.Single();
        Assert.AreEqual(AnsiSgrOp.SetForeground(AnsiColor.Indexed256(123)), sgr.Operations.Single());
    }

    [TestMethod]
    public void Background_TrueColor_ParsesAsSgr()
    {
        var output = AnsiRoundTrip.Emit(w => w.Background(AnsiColor.Rgb(1, 2, 3)));
        Assert.AreEqual("\x1b[48;2;1;2;3m", output);

        var tokens = AnsiRoundTrip.EmitAndTokenize(w => w.Background(AnsiColor.Rgb(1, 2, 3)));
        var sgr = (SgrToken)tokens.Single();
        Assert.AreEqual(AnsiSgrOp.SetBackground(AnsiColor.Rgb(1, 2, 3)), sgr.Operations.Single());
    }

    [TestMethod]
    public void CursorUp_ParsesAsCsi()
    {
        var output = AnsiRoundTrip.Emit(w => w.CursorUp(3));
        Assert.AreEqual("\x1b[3A", output);

        var csi = (CsiToken)AnsiRoundTrip.EmitAndTokenize(w => w.CursorUp(3)).Single();
        Assert.AreEqual('A', csi.Final);
        CollectionAssert.AreEqual(new[] { 3 }, csi.Parameters);
    }

    [TestMethod]
    public void CursorDownForwardBack_ParseAsCsi()
    {
        var tokens = AnsiRoundTrip.EmitAndTokenize(w =>
        {
            w.CursorDown(2);
            w.CursorForward(3);
            w.CursorBack(4);
        });

        Assert.HasCount(3, tokens);
        Assert.AreEqual('B', ((CsiToken)tokens[0]).Final);
        CollectionAssert.AreEqual(new[] { 2 }, ((CsiToken)tokens[0]).Parameters);

        Assert.AreEqual('C', ((CsiToken)tokens[1]).Final);
        CollectionAssert.AreEqual(new[] { 3 }, ((CsiToken)tokens[1]).Parameters);

        Assert.AreEqual('D', ((CsiToken)tokens[2]).Final);
        CollectionAssert.AreEqual(new[] { 4 }, ((CsiToken)tokens[2]).Parameters);
    }

    [TestMethod]
    public void CursorPosition_ParsesAsCsi()
    {
        var output = AnsiRoundTrip.Emit(w => w.CursorPosition(2, 5));
        Assert.AreEqual("\x1b[2;5H", output);

        var csi = (CsiToken)AnsiRoundTrip.EmitAndTokenize(w => w.CursorPosition(2, 5)).Single();
        Assert.AreEqual('H', csi.Final);
        CollectionAssert.AreEqual(new[] { 2, 5 }, csi.Parameters);
    }

    [TestMethod]
    public void EraseInLine_And_EraseInDisplay_ParseAsCsi()
    {
        var tokens = AnsiRoundTrip.EmitAndTokenize(w =>
        {
            w.EraseInLine(0);
            w.EraseInDisplay(2);
        });

        Assert.HasCount(2, tokens);

        var csi1 = (CsiToken)tokens[0];
        Assert.AreEqual('K', csi1.Final);
        CollectionAssert.AreEqual(Array.Empty<int>(), csi1.Parameters);

        var csi2 = (CsiToken)tokens[1];
        Assert.AreEqual('J', csi2.Final);
        CollectionAssert.AreEqual(new[] { 2 }, csi2.Parameters);
    }

    [TestMethod]
    public void ShowCursor_ParsesAsDecPrivateModeCsi()
    {
        var tokens = AnsiRoundTrip.EmitAndTokenize(w =>
        {
            w.ShowCursor(visible: false);
            w.ShowCursor(visible: true);
        });

        Assert.HasCount(2, tokens);

        var hide = (CsiToken)tokens[0];
        Assert.AreEqual('l', hide.Final);
        Assert.AreEqual('?', hide.PrivateMarker);
        CollectionAssert.AreEqual(new[] { 25 }, hide.Parameters);

        var show = (CsiToken)tokens[1];
        Assert.AreEqual('h', show.Final);
        Assert.AreEqual('?', show.PrivateMarker);
        CollectionAssert.AreEqual(new[] { 25 }, show.Parameters);
    }

    [TestMethod]
    public void SaveAndRestoreCursor_ParseAsEsc()
    {
        var tokens = AnsiRoundTrip.EmitAndTokenize(w =>
        {
            w.SaveCursor();
            w.RestoreCursor();
        });

        Assert.HasCount(2, tokens);

        var save = (EscToken)tokens[0];
        Assert.AreEqual('7', save.Final);
        Assert.AreEqual(string.Empty, save.Intermediates);

        var restore = (EscToken)tokens[1];
        Assert.AreEqual('8', restore.Final);
        Assert.AreEqual(string.Empty, restore.Intermediates);
    }

    [TestMethod]
    public void SoftReset_ParsesAsCsiWithIntermediate()
    {
        var csi = (CsiToken)AnsiRoundTrip.EmitAndTokenize(w => w.SoftReset()).Single();
        Assert.AreEqual('p', csi.Final);
        Assert.AreEqual("!", csi.Intermediates);
        CollectionAssert.AreEqual(Array.Empty<int>(), csi.Parameters);
    }

    [TestMethod]
    public void AlternateScreen_ParseAsDecPrivateModeCsi()
    {
        var tokens = AnsiRoundTrip.EmitAndTokenize(w =>
        {
            w.EnterAlternateScreen();
            w.LeaveAlternateScreen();
        });

        Assert.HasCount(2, tokens);

        var enter = (CsiToken)tokens[0];
        Assert.AreEqual('h', enter.Final);
        Assert.AreEqual('?', enter.PrivateMarker);
        CollectionAssert.AreEqual(new[] { 1049 }, enter.Parameters);

        var leave = (CsiToken)tokens[1];
        Assert.AreEqual('l', leave.Final);
        Assert.AreEqual('?', leave.PrivateMarker);
        CollectionAssert.AreEqual(new[] { 1049 }, leave.Parameters);
    }

    [TestMethod]
    public void Osc8Hyperlink_EmitsBeginAndEnd_AndParsesAsOsc()
    {
        var caps = AnsiCapabilities.Default with { OscTermination = AnsiOscTermination.StringTerminator };
        var output = AnsiRoundTrip.Emit(w =>
        {
            w.BeginLink("https://example.com");
            w.EndLink();
        }, caps);

        Assert.AreEqual("\x1b]8;;https://example.com\x1b\\\x1b]8;;\x1b\\", output);

        using var tokenizer = new AnsiTokenizer();
        var tokens = tokenizer.Tokenize(output.AsSpan());
        Assert.HasCount(2, tokens);
        Assert.IsInstanceOfType<OscToken>(tokens[0]);
        Assert.IsInstanceOfType<OscToken>(tokens[1]);

        var begin = (OscToken)tokens[0];
        Assert.AreEqual(8, begin.Code);
        Assert.AreEqual(";https://example.com", begin.Data);

        var end = (OscToken)tokens[1];
        Assert.AreEqual(8, end.Code);
        Assert.AreEqual(";", end.Data);
    }

    [TestMethod]
    public void SetStyle_ParsesAsSgr()
    {
        var style = new AnsiStyle
        {
            Foreground = AnsiColors.Red,
            Background = AnsiColors.Default,
            Decorations = AnsiDecorations.Underline,
        };

        var output = AnsiRoundTrip.Emit(w => w.Style(style));
        Assert.AreEqual("\x1b[4;31m", output);

        var sgr = (SgrToken)AnsiRoundTrip.EmitAndTokenize(w => w.Style(style)).Single();
        CollectionAssert.AreEqual(
            new[]
            {
                AnsiSgrOp.SetDecoration(AnsiDecorations.Underline, enabled: true),
                AnsiSgrOp.SetForeground(AnsiColor.Basic16(1)),
            },
            sgr.Operations);
    }

    [TestMethod]
    public void SetDecorations_ParsesAsSgr()
    {
        var tokens = AnsiRoundTrip.EmitAndTokenize(w =>
        {
            w.Decorate(AnsiDecorations.Bold | AnsiDecorations.Italic);
            w.Undecorate(AnsiDecorations.Italic);
        });

        Assert.HasCount(2, tokens);

        var sgr1 = (SgrToken)tokens[0];
        CollectionAssert.AreEqual(
            new[]
            {
                AnsiSgrOp.SetDecoration(AnsiDecorations.Bold, enabled: true),
                AnsiSgrOp.SetDecoration(AnsiDecorations.Italic, enabled: true),
            },
            sgr1.Operations);

        var sgr2 = (SgrToken)tokens[1];
        CollectionAssert.AreEqual(
            new[] { AnsiSgrOp.SetDecoration(AnsiDecorations.Italic, enabled: false) },
            sgr2.Operations);
    }

    [TestMethod]
    public void WriteStyleTransition_TreatsNullColorsAsNoOp_AndParsesAsSgr()
    {
        var from = new AnsiStyle { Foreground = AnsiColor.Basic16(1), Background = AnsiColor.Default, Decorations = AnsiDecorations.Bold };
        var to = new AnsiStyle { Foreground = null, Background = null, Decorations = AnsiDecorations.None };

        var output = AnsiRoundTrip.Emit(w => w.WriteStyleTransition(from, to));
        Assert.AreEqual("\x1b[22m", output);

        var sgr = (SgrToken)AnsiRoundTrip.EmitAndTokenize(w => w.WriteStyleTransition(from, to)).Single();
        CollectionAssert.AreEqual(
            new[]
            {
                AnsiSgrOp.SetDecoration(AnsiDecorations.Bold, enabled: false),
                AnsiSgrOp.SetDecoration(AnsiDecorations.Dim, enabled: false),
            },
            sgr.Operations);
    }
}
