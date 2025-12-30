// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiStyleTests
{
    [TestMethod]
    public void WithForeground_Background_AndDecorations_ReturnUpdatedCopies()
    {
        var style = AnsiStyle.Default;
        style = style.WithForeground(AnsiColors.Red);
        style = style.WithBackground(AnsiColors.Blue);
        style = style.WithDecorations(AnsiDecorations.Bold);

        Assert.AreEqual(AnsiColors.Red, style.Foreground);
        Assert.AreEqual(AnsiColors.Blue, style.Background);
        Assert.AreEqual(AnsiDecorations.Bold, style.Decorations);
    }
}

