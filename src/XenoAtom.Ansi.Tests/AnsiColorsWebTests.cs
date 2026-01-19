// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiColorsWebTests
{
    [TestMethod]
    public void TryGetWebByName_FindsNamedColor()
    {
        Assert.IsTrue(AnsiColors.TryGetWebByName("cornflowerblue".AsSpan(), out var color));
        Assert.AreEqual(AnsiColor.Rgb(100, 149, 237), color);
    }

    [TestMethod]
    public void TryGetWebByName_AllowsSeparators()
    {
        Assert.IsTrue(AnsiColors.TryGetWebByName("cornflower-blue".AsSpan(), out var color));
        Assert.AreEqual(AnsiColor.Rgb(100, 149, 237), color);
    }

    [TestMethod]
    public void TryGetByName_PrefersBasicPaletteForLegacyNames()
    {
        Assert.IsTrue(AnsiColors.TryGetByName("gray".AsSpan(), out var color));
        Assert.AreEqual(AnsiColors.BrightBlack, color);
    }

    [TestMethod]
    public void TryGetByName_CanForceWebColorsWithPrefix()
    {
        Assert.IsTrue(AnsiColors.TryGetByName("web:gray".AsSpan(), out var color));
        Assert.AreEqual(AnsiColor.Rgb(128, 128, 128), color);
    }

    [TestMethod]
    public void Markup_SupportsWebNamedColors()
    {
        var actual = AnsiMarkup.Render("[cornflowerblue]X[/]");

        var baseStyle = AnsiStyle.Default;
        var styled = baseStyle with { Foreground = AnsiColor.Rgb(100, 149, 237) };
        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(baseStyle, styled);
            w.Write("X");
            w.StyleTransition(styled, baseStyle);
        });

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Markup_CanForceWebColorsWithPrefix()
    {
        var actual = AnsiMarkup.Render("[web:gray]X[/]");

        var baseStyle = AnsiStyle.Default;
        var styled = baseStyle with { Foreground = AnsiColor.Rgb(128, 128, 128) };
        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(baseStyle, styled);
            w.Write("X");
            w.StyleTransition(styled, baseStyle);
        });

        Assert.AreEqual(expected, actual);
    }
}

