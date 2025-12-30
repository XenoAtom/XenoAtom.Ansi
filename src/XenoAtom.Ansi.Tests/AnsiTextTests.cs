// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiTextTests
{
    [TestMethod]
    public void Strip_RemovesEscapes()
    {
        var input = "a\x1b[31mb";
        Assert.AreEqual("ab", AnsiText.Strip(input.AsSpan()));
    }

    [TestMethod]
    public void GetVisibleWidth_Cjk_IsWide()
    {
        Assert.AreEqual(4, AnsiText.GetVisibleWidth("a界b".AsSpan()));
    }

    [TestMethod]
    public void Wrap_HardWrap_NoAnsi()
    {
        var lines = AnsiText.Wrap("abcd", 2, preserveAnsi: false);
        CollectionAssert.AreEqual(new[] { "ab", "cd" }, lines.ToArray());
    }

    [TestMethod]
    public void Wrap_PreservesStyle()
    {
        var input = "ab\x1b[31mcd\x1b[0mef";
        var lines = AnsiText.Wrap(input, 2, preserveAnsi: true);
        CollectionAssert.AreEqual(new[] { "ab\x1b[0m", "\x1b[31mcd\x1b[0m", "ef\x1b[0m" }, lines.ToArray());
    }

    [TestMethod]
    public void Truncate_PreservesAnsi_WhenCutInStyledRun()
    {
        var input = "ab\x1b[31mcd\x1b[0mef";
        var truncated = AnsiText.Truncate(input, 4, preserveAnsi: true);
        Assert.AreEqual("ab\x1b[31mc…\x1b[0m", truncated);
    }
}

