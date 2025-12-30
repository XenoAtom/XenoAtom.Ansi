// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiMarkupInterpolatedStringHandlerTests
{
    private readonly struct UnformattableSpan : ISpanFormattable
    {
        private readonly string _text;

        public UnformattableSpan(string text) => _text = text;

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            charsWritten = 0;
            return false;
        }

        public string ToString(string? format, IFormatProvider? formatProvider) => _text;
    }

    [TestMethod]
    public void Render_Interpolated_EscapesStringValue()
    {
        var value = "[x]";
        var actual = AnsiMarkup.Render($"a{value}b");
        Assert.AreEqual("a[x]b", actual);
    }

    [TestMethod]
    public void Render_Interpolated_NullString_IsEmpty()
    {
        string? value = null;
        var actual = AnsiMarkup.Render($"a{value}b");
        Assert.AreEqual("ab", actual);
    }

    [TestMethod]
    public void Render_Interpolated_UnformattableSpan_FallsBackToToString_AndEscapes()
    {
        var value = new UnformattableSpan("[x]");
        var actual = AnsiMarkup.Render($"a{value}b");
        Assert.AreEqual("a[x]b", actual);
    }

    [TestMethod]
    public void Render_Interpolated_EscapesReadOnlySpanValue()
    {
        ReadOnlySpan<char> value = "[x]".AsSpan();
        var actual = AnsiMarkup.Render($"a{value}b");
        Assert.AreEqual("a[x]b", actual);
    }

    [TestMethod]
    public void Render_Interpolated_EscapesCharValue()
    {
        var actual = AnsiMarkup.Render($"a{'['}{']'}b");
        Assert.AreEqual("a[]b", actual);
    }

    [TestMethod]
    public void Render_Interpolated_EscapesObjectValue()
    {
        object value = "[x]";
        var actual = AnsiMarkup.Render($"a{value}b");
        Assert.AreEqual("a[x]b", actual);
    }

    [TestMethod]
    public void Render_Interpolated_SupportsAlignment_ForString()
    {
        var value = "x";
        Assert.AreEqual("  x", AnsiMarkup.Render($"{value,3}"));
        Assert.AreEqual("x  ", AnsiMarkup.Render($"{value,-3}"));
    }

    [TestMethod]
    public void Render_Interpolated_SupportsAlignment_ForEscapedString()
    {
        var value = "[";
        Assert.AreEqual("  [", AnsiMarkup.Render($"{value,3}"));
        Assert.AreEqual("[  ", AnsiMarkup.Render($"{value,-3}"));
    }

    [TestMethod]
    public void Render_Interpolated_SupportsAlignment_ForReadOnlySpan()
    {
        ReadOnlySpan<char> value = "x".AsSpan();
        Assert.AreEqual("  x", AnsiMarkup.Render($"{value,3}"));
        Assert.AreEqual("x  ", AnsiMarkup.Render($"{value,-3}"));
    }

    [TestMethod]
    public void Render_Interpolated_SupportsAlignment_ForChar()
    {
        Assert.AreEqual("  x", AnsiMarkup.Render($"{'x',3}"));
        Assert.AreEqual("x  ", AnsiMarkup.Render($"{'x',-3}"));
    }

    [TestMethod]
    public void Render_Interpolated_SupportsAlignment_ForEscapedChar()
    {
        Assert.AreEqual("  [", AnsiMarkup.Render($"{'[',3}"));
        Assert.AreEqual("[  ", AnsiMarkup.Render($"{'[',-3}"));
    }

    [TestMethod]
    public void Render_Interpolated_SupportsAlignment_ForSpanFormattable()
    {
        var value = 42;
        Assert.AreEqual("   42", AnsiMarkup.Render($"{value,5}"));
        Assert.AreEqual("42   ", AnsiMarkup.Render($"{value,-5}"));
    }

    [TestMethod]
    public void Render_Interpolated_SupportsFormat_ForSpanFormattable()
    {
        var value = 42;
        Assert.AreEqual("0042", AnsiMarkup.Render($"{value:D4}"));
        Assert.AreEqual("  0042", AnsiMarkup.Render($"{value,6:D4}"));
        Assert.AreEqual("0042  ", AnsiMarkup.Render($"{value,-6:D4}"));
    }

    [TestMethod]
    public void Render_Interpolated_SupportsAlignment_ForObject()
    {
        object value = "x";
        Assert.AreEqual("  x", AnsiMarkup.Render($"{value,3}"));
        Assert.AreEqual("x  ", AnsiMarkup.Render($"{value,-3}"));
    }
}
