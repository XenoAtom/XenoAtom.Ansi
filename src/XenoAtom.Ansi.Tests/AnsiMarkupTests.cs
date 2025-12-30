// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiMarkupTests
{
    [TestMethod]
    public void Render_Foreground_And_Close()
    {
        var actual = AnsiMarkup.Render("[red]X[/]");

        var baseStyle = AnsiStyle.Default;
        var styled = baseStyle with { Foreground = AnsiColors.Red };
        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(baseStyle, styled);
            w.Write("X");
            w.StyleTransition(styled, baseStyle);
        });

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Render_Interpolated_Escapes_InjectedMarkup()
    {
        var userInput = "[red]X[/]";

        var actual = AnsiMarkup.Render($"a{userInput}b");
        Assert.AreEqual("a[red]X[/]b", actual);
    }

    [TestMethod]
    public void Render_Interpolated_AllowsMarkupAroundEscapedValue()
    {
        var userInput = "[red]X[/]";

        var actual = AnsiMarkup.Render($"[red]{userInput}[/]");

        var baseStyle = AnsiStyle.Default;
        var styled = baseStyle with { Foreground = AnsiColors.Red };
        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(baseStyle, styled);
            w.Write("[red]X[/]");
            w.StyleTransition(styled, baseStyle);
        });

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Render_Interpolated_Instance_UsesHandlerAndEscapes()
    {
        var userInput = "[blue]X[/]";

        using var builder = new AnsiBuilder();
        var writer = new AnsiWriter(builder);
        var renderer = new AnsiMarkup(writer);
        renderer.Write($"[green]{userInput}[/]");
        var actual = builder.ToString();

        var baseStyle = AnsiStyle.Default;
        var styled = baseStyle with { Foreground = AnsiColors.Green };
        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(baseStyle, styled);
            w.Write("[blue]X[/]");
            w.StyleTransition(styled, baseStyle);
        });

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Append_WithExternalWriter_WritesToProvidedTarget()
    {
        using var builder = new AnsiBuilder();
        var writer = new AnsiWriter(builder);
        var markup = new AnsiMarkup(writer);

        markup.Write("[red]X[/]".AsSpan());

        var baseStyle = AnsiStyle.Default;
        var styled = baseStyle with { Foreground = AnsiColors.Red };
        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(baseStyle, styled);
            w.Write("X");
            w.StyleTransition(styled, baseStyle);
        });

        Assert.AreEqual(expected, builder.ToString());
    }

    [TestMethod]
    public void Render_Decorations_And_Background()
    {
        var actual = AnsiMarkup.Render("[bold yellow on blue]X[/]");

        var baseStyle = AnsiStyle.Default;
        var styled = baseStyle with { Decorations = AnsiDecorations.Bold, Foreground = AnsiColors.Yellow, Background = AnsiColors.Blue };
        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(baseStyle, styled);
            w.Write("X");
            w.StyleTransition(styled, baseStyle);
        });

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Render_Nested_Tags()
    {
        var actual = AnsiMarkup.Render("[red]a[blue]b[/]c[/]");

        var s0 = AnsiStyle.Default;
        var sRed = s0 with { Foreground = AnsiColors.Red };
        var sBlue = s0 with { Foreground = AnsiColors.Blue };

        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(s0, sRed);
            w.Write("a");
            w.StyleTransition(sRed, sBlue);
            w.Write("b");
            w.StyleTransition(sBlue, sRed);
            w.Write("c");
            w.StyleTransition(sRed, s0);
        });

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Render_Escapes_Brackets()
    {
        var actual = AnsiMarkup.Render("a[[b]]c");
        Assert.AreEqual("a[b]c", actual);
    }

    [TestMethod]
    public void WriteEscape_WritesTextVerbatim_WithoutInterpretingMarkup()
    {
        using var builder = new AnsiBuilder();
        var writer = new AnsiWriter(builder);
        var markup = new AnsiMarkup(writer);

        markup.WriteEscape("[red]X[/]");

        Assert.AreEqual("[red]X[/]", builder.ToString());
    }

    [TestMethod]
    public void AppendTo_Static_WritesToProvidedWriter()
    {
        using var builder = new AnsiBuilder();
        var writer = new AnsiWriter(builder);

        AnsiMarkup.AppendTo(writer, "[red]X[/]".AsSpan());

        var baseStyle = AnsiStyle.Default;
        var redStyle = baseStyle with { Foreground = AnsiColors.Red };
        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(baseStyle, redStyle);
            w.Write("X");
            w.StyleTransition(redStyle, baseStyle);
        });

        Assert.AreEqual(expected, builder.ToString());
    }

    [TestMethod]
    public void WriteEscape_ReadOnlySpan_UsesVerbatimWrite()
    {
        using var builder = new AnsiBuilder();
        var writer = new AnsiWriter(builder);
        var markup = new AnsiMarkup(writer);

        markup.WriteEscape("[x]".AsSpan());

        Assert.AreEqual("[x]", builder.ToString());
    }

    [TestMethod]
    public void WriteEscape_CanBeChainedWithWrite()
    {
        using var builder = new AnsiBuilder();
        var writer = new AnsiWriter(builder);
        var markup = new AnsiMarkup(writer);

        markup.Write("[red]X[/]").WriteEscape("Y");

        var baseStyle = AnsiStyle.Default;
        var redStyle = baseStyle with { Foreground = AnsiColors.Red };
        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(baseStyle, redStyle);
            w.Write("X");
            w.StyleTransition(redStyle, baseStyle);
            w.Write("Y");
        });

        Assert.AreEqual(expected, builder.ToString());
    }

    [TestMethod]
    public void Escape_Escapes_Brackets()
    {
        var actual = AnsiMarkup.Escape("a[b]c".AsSpan());
        Assert.AreEqual("a[[b]]c", actual);
    }

    [TestMethod]
    public void Render_Unknown_Tag_Is_Literal()
    {
        var actual = AnsiMarkup.Render("a[nope]b");
        Assert.AreEqual("a[nope]b", actual);
    }

    [TestMethod]
    public void Render_HexColors()
    {
        var actual = AnsiMarkup.Render("[#ff8000 bg:#010203]X[/]");

        var baseStyle = AnsiStyle.Default;
        var styled = baseStyle with { Foreground = AnsiColor.Rgb(255, 128, 0), Background = AnsiColor.Rgb(1, 2, 3) };
        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(baseStyle, styled);
            w.Write("X");
            w.StyleTransition(styled, baseStyle);
        });

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Render_Indexed256()
    {
        var actual = AnsiMarkup.Render("[196]X[/]");

        var baseStyle = AnsiStyle.Default;
        var styled = baseStyle with { Foreground = AnsiColor.Indexed256(196) };
        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(baseStyle, styled);
            w.Write("X");
            w.StyleTransition(styled, baseStyle);
        });

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Render_RgbFunction()
    {
        var actual = AnsiMarkup.Render("[rgb(1, 2, 3)]X[/]");

        var baseStyle = AnsiStyle.Default;
        var styled = baseStyle with { Foreground = AnsiColor.Rgb(1, 2, 3) };
        var expected = AnsiRoundTrip.Emit(w =>
        {
            w.StyleTransition(baseStyle, styled);
            w.Write("X");
            w.StyleTransition(styled, baseStyle);
        });

        Assert.AreEqual(expected, actual);
    }
}
