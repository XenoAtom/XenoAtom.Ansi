// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiStyledTextParserTests
{
    [TestMethod]
    public void Parse_PlainText_ProducesSingleRun()
    {
        using var parser = new AnsiStyledTextParser();
        var runs = parser.Parse("hello".AsSpan());

        Assert.HasCount(1, runs);
        Assert.AreEqual("hello", runs[0].Text);
        Assert.AreEqual(AnsiStyle.Default, runs[0].Style);
        Assert.IsNull(runs[0].Hyperlink);
    }

    [TestMethod]
    public void Parse_Sgr_ProducesStyledRuns()
    {
        var s = AnsiRoundTrip.Emit(w =>
        {
            w.Write("a");
            w.Foreground(AnsiColors.Red);
            w.Write("b");
            w.Reset();
            w.Write("c");
        });

        using var parser = new AnsiStyledTextParser();
        var runs = parser.Parse(s.AsSpan());

        Assert.HasCount(3, runs);
        Assert.AreEqual("a", runs[0].Text);
        Assert.AreEqual(AnsiStyle.Default, runs[0].Style);
        Assert.AreEqual("b", runs[1].Text);
        Assert.AreEqual(AnsiColors.Red, runs[1].Style.Foreground);
        Assert.AreEqual("c", runs[2].Text);
        Assert.AreEqual(AnsiStyle.Default, runs[2].Style);
    }

    [TestMethod]
    public void Parse_Osc8Hyperlink_TracksLinkPerRun_AndEndsOnClose()
    {
        var caps = AnsiCapabilities.Default with { OscTermination = AnsiOscTermination.StringTerminator };
        var s = AnsiRoundTrip.Emit(w =>
        {
            w.BeginLink("https://example.com", "myid");
            w.Write("x");
            w.EndLink();
            w.Write("y");
        }, caps);

        using var parser = new AnsiStyledTextParser();
        var runs = parser.Parse(s.AsSpan());

        Assert.HasCount(2, runs);
        Assert.AreEqual("x", runs[0].Text);
        Assert.IsNotNull(runs[0].Hyperlink);
        Assert.AreEqual("https://example.com", runs[0].Hyperlink!.Value.Uri);
        Assert.AreEqual("myid", runs[0].Hyperlink!.Value.Id);

        Assert.AreEqual("y", runs[1].Text);
        Assert.IsNull(runs[1].Hyperlink);
    }

    [TestMethod]
    public void Parse_InvalidOsc8_NoSecondSemicolon_DoesNotChangeHyperlink()
    {
        // OSC 8 requires: ESC ] 8 ; params ; uri ST
        // Here we provide only a single ';' after the code, so parsing should ignore it.
        var s = "\x1b]8;https://example.com\x1b\\X";

        using var parser = new AnsiStyledTextParser();
        var runs = parser.Parse(s.AsSpan());

        Assert.HasCount(1, runs);
        Assert.AreEqual("X", runs[0].Text);
        Assert.IsNull(runs[0].Hyperlink);
    }
}
