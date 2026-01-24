// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiMarkupCustomTokensTests
{
    [TestMethod]
    public void CustomToken_CanApplyStyle()
    {
        var customStyles = new Dictionary<string, AnsiStyle>(StringComparer.OrdinalIgnoreCase)
        {
            ["primary"] = new AnsiStyle { Foreground = AnsiColor.Rgb(100, 149, 237), Decorations = AnsiDecorations.Bold },
        };

        using var builder = new AnsiBuilder();
        var writer = new AnsiWriter(builder);
        var markup = new AnsiMarkup(writer, customStyles);
        markup.Write("[primary]X[/]");

        var baseStyle = AnsiStyle.Default;
        var expectedStyle = baseStyle with { Foreground = AnsiColor.Rgb(100, 149, 237), Decorations = AnsiDecorations.Bold };
        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(baseStyle, expectedStyle);
            w.Write("X");
            w.StyleTransition(expectedStyle, baseStyle);
        });

        Assert.AreEqual(expected, builder.ToString());
    }

    [TestMethod]
    public void CustomToken_CanBeUsedAsBackgroundColor()
    {
        var customStyles = new Dictionary<string, AnsiStyle>(StringComparer.OrdinalIgnoreCase)
        {
            ["primary"] = new AnsiStyle { Foreground = AnsiColor.Rgb(1, 2, 3) },
        };

        using var builder = new AnsiBuilder();
        var writer = new AnsiWriter(builder);
        var markup = new AnsiMarkup(writer, customStyles);
        markup.Write("[on primary]X[/]");

        var baseStyle = AnsiStyle.Default;
        var expectedStyle = baseStyle with { Background = AnsiColor.Rgb(1, 2, 3) };
        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(baseStyle, expectedStyle);
            w.Write("X");
            w.StyleTransition(expectedStyle, baseStyle);
        });

        Assert.AreEqual(expected, builder.ToString());
    }

    [TestMethod]
    public void CustomTokens_DoNotOverrideBuiltInColorNames()
    {
        var customStyles = new Dictionary<string, AnsiStyle>(StringComparer.OrdinalIgnoreCase)
        {
            ["red"] = new AnsiStyle { Foreground = AnsiColor.Rgb(1, 2, 3) },
        };

        using var builder = new AnsiBuilder();
        var writer = new AnsiWriter(builder);
        var markup = new AnsiMarkup(writer, customStyles);
        markup.Write("[red]X[/]");

        var baseStyle = AnsiStyle.Default;
        var expectedStyle = baseStyle with { Foreground = AnsiColors.Red };
        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(baseStyle, expectedStyle);
            w.Write("X");
            w.StyleTransition(expectedStyle, baseStyle);
        });

        Assert.AreEqual(expected, builder.ToString());
    }
}

