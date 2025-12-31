// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi.Tokens;

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class CsiTokenExtensionsTests
{
    [TestMethod]
    public void TryGetCursorStyle_ReturnsTrue_ForWriterOutput()
    {
        var token = (CsiToken)AnsiRoundTrip.EmitAndTokenize(w => w.CursorStyle(AnsiCursorStyle.SteadyBar)).Single();
        Assert.IsTrue(token.TryGetCursorStyle(out var style));
        Assert.AreEqual(AnsiCursorStyle.SteadyBar, style);
    }

    [TestMethod]
    public void TryGetCursorStyle_ReturnsTrue_ForEmptyParameters_AsDefault()
    {
        var token = new CsiToken(" ", Array.Empty<int>(), 'q');
        Assert.IsTrue(token.TryGetCursorStyle(out var style));
        Assert.AreEqual(AnsiCursorStyle.Default, style);
    }

    [TestMethod]
    public void TryGetCursorStyle_ReturnsFalse_ForWrongIntermediate()
    {
        var token = new CsiToken("", new[] { 3 }, 'q');
        Assert.IsFalse(token.TryGetCursorStyle(out _));
    }

    [TestMethod]
    public void TryGetCursorStyle_ReturnsFalse_ForMultipleParameters()
    {
        var token = new CsiToken(" ", new[] { 3, 4 }, 'q');
        Assert.IsFalse(token.TryGetCursorStyle(out _));
    }

    [TestMethod]
    public void TryGetCursorStyle_ReturnsFalse_ForOutOfRangeValue()
    {
        var token = new CsiToken(" ", new[] { 99 }, 'q');
        Assert.IsFalse(token.TryGetCursorStyle(out _));
    }
}

