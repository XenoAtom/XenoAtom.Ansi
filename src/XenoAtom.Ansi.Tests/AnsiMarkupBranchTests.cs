// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiMarkupBranchTests
{
    [TestMethod]
    public void Render_Parses_AllBasicColorNames_AndAliases()
    {
        var cases = new (string Token, AnsiColor Color)[]
        {
            ("black", AnsiColors.Black),
            ("red", AnsiColors.Red),
            ("green", AnsiColors.Green),
            ("yellow", AnsiColors.Yellow),
            ("blue", AnsiColors.Blue),
            ("magenta", AnsiColors.Magenta),
            ("purple", AnsiColors.Magenta),
            ("cyan", AnsiColors.Cyan),
            ("aqua", AnsiColors.Cyan),
            ("white", AnsiColors.White),
            ("gray", AnsiColors.BrightBlack),
            ("grey", AnsiColors.BrightBlack),
            ("default", AnsiColor.Default),
        };

        foreach (var (token, color) in cases)
        {
            var actual = AnsiMarkup.Render($"[{token}]X[/]".AsSpan());

            var baseStyle = AnsiStyle.Default;
            var styled = baseStyle with { Foreground = color };
            var expected = AnsiRoundTrip.Emit(w =>
            {
                w.StyleTransition(baseStyle, styled);
                w.Write("X");
                w.StyleTransition(styled, baseStyle);
            });

            Assert.AreEqual(expected, actual, $"Token: {token}");
        }
    }

    [TestMethod]
    public void Render_Parses_BrightAndLightColorPrefixes()
    {
        var cases = new (string Token, AnsiColor Color)[]
        {
            ("brightred", AnsiColors.BrightRed),
            ("bright-red", AnsiColors.BrightRed),
            ("bright:red", AnsiColors.BrightRed),
            ("lightblue", AnsiColors.BrightBlue),
            ("light-blue", AnsiColors.BrightBlue),
        };

        foreach (var (token, color) in cases)
        {
            var actual = AnsiMarkup.Render($"[{token}]X[/]".AsSpan());

            var baseStyle = AnsiStyle.Default;
            var styled = baseStyle with { Foreground = color };
            var expected = AnsiRoundTrip.Emit(w =>
            {
                w.StyleTransition(baseStyle, styled);
                w.Write("X");
                w.StyleTransition(styled, baseStyle);
            });

            Assert.AreEqual(expected, actual, $"Token: {token}");
        }
    }

    [TestMethod]
    public void Render_Parses_BackgroundForms()
    {
        var cases = new[]
        {
            ("[on blue]X[/]", AnsiColors.Blue),
            ("[bg:blue]X[/]", AnsiColors.Blue),
            ("[bg=blue]X[/]", AnsiColors.Blue),
            ("[bg:196]X[/]", AnsiColor.Indexed256(196)),
            ("[bg:#010203]X[/]", AnsiColor.Rgb(1, 2, 3)),
            ("[bg:rgb(1, 2, 3)]X[/]", AnsiColor.Rgb(1, 2, 3)),
        };

        foreach (var (markup, bg) in cases)
        {
            var actual = AnsiMarkup.Render(markup.AsSpan());

            var baseStyle = AnsiStyle.Default;
            var styled = baseStyle with { Background = bg };
            var expected = AnsiRoundTrip.Emit(w =>
            {
                w.StyleTransition(baseStyle, styled);
                w.Write("X");
                w.StyleTransition(styled, baseStyle);
            });

            Assert.AreEqual(expected, actual, $"Markup: {markup}");
        }
    }

    [TestMethod]
    public void Render_Parses_ForegroundPrefixes()
    {
        var cases = new[]
        {
            ("[fg:red]X[/]", AnsiColors.Red),
            ("[fg=red]X[/]", AnsiColors.Red),
            ("[fg:#ff8000]X[/]", AnsiColor.Rgb(255, 128, 0)),
            ("[fg:rgb(1,2,3)]X[/]", AnsiColor.Rgb(1, 2, 3)),
            ("[fg:196]X[/]", AnsiColor.Indexed256(196)),
        };

        foreach (var (markup, fg) in cases)
        {
            var actual = AnsiMarkup.Render(markup.AsSpan());

            var baseStyle = AnsiStyle.Default;
            var styled = baseStyle with { Foreground = fg };
            var expected = AnsiRoundTrip.Emit(w =>
            {
                w.StyleTransition(baseStyle, styled);
                w.Write("X");
                w.StyleTransition(styled, baseStyle);
            });

            Assert.AreEqual(expected, actual, $"Markup: {markup}");
        }
    }

    [TestMethod]
    public void Render_Parses_AllDecorations()
    {
        var actual = AnsiMarkup.Render("[bold dim italic underline blink invert hidden strikethrough]X[/]".AsSpan());

        var baseStyle = AnsiStyle.Default;
        var styled = baseStyle with
        {
            Decorations =
                AnsiDecorations.Bold |
                AnsiDecorations.Dim |
                AnsiDecorations.Italic |
                AnsiDecorations.Underline |
                AnsiDecorations.Blink |
                AnsiDecorations.Invert |
                AnsiDecorations.Hidden |
                AnsiDecorations.Strikethrough
        };
        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(baseStyle, styled);
            w.Write("X");
            w.StyleTransition(styled, baseStyle);
        });

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Render_ResetTag_Transitions_ToDefault_AndPreservesStack()
    {
        var actual = AnsiMarkup.Render("[red]a[reset]b[/]c[/]".AsSpan());

        var baseStyle = AnsiStyle.Default;
        var red = baseStyle with { Foreground = AnsiColors.Red };
        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(baseStyle, red);
            w.Write("a");
            w.StyleTransition(red, baseStyle);
            w.Write("b");
            w.StyleTransition(baseStyle, red);
            w.Write("c");
            w.StyleTransition(red, baseStyle);
        });

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Render_CloseTag_WhenNoOpenTag_IsIgnored()
    {
        Assert.AreEqual("ab", AnsiMarkup.Render("a[/]b".AsSpan()));
        Assert.AreEqual("ab", AnsiMarkup.Render("a[/nope]b".AsSpan()));
    }

    [TestMethod]
    public void Render_UnclosedOpenBracket_IsLiteral()
    {
        Assert.AreEqual("a[red", AnsiMarkup.Render("a[red".AsSpan()));
    }

    [TestMethod]
    public void Render_Escapes_ClosingBracket()
    {
        Assert.AreEqual("a]b", AnsiMarkup.Render("a]]b".AsSpan()));
    }

    [TestMethod]
    public void Render_InvalidColorToken_IsLiteralTag()
    {
        Assert.AreEqual("[rgb(256,0,0)]X", AnsiMarkup.Render("[rgb(256,0,0)]X".AsSpan()));
        Assert.AreEqual("[256]X", AnsiMarkup.Render("[256]X".AsSpan()));
    }

    [TestMethod]
    public void Render_InvalidOnSyntax_IsLiteralTag()
    {
        Assert.AreEqual("[on]X", AnsiMarkup.Render("[on]X".AsSpan()));
        Assert.AreEqual("[on nope]X", AnsiMarkup.Render("[on nope]X".AsSpan()));
    }
}
