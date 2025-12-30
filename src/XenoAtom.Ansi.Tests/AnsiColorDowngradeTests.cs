// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiColorDowngradeTests
{
    [TestMethod]
    public void TryDowngrade_Rgb_To256_ReturnsIndexed256()
    {
        var rgb = AnsiColor.Rgb(1, 2, 3);
        Assert.IsTrue(rgb.TryDowngrade(AnsiColorLevel.Colors256, out var downgraded));
        Assert.AreEqual(AnsiColorKind.Indexed256, downgraded.Kind);
    }

    [TestMethod]
    public void TryDowngrade_Rgb_To16_ReturnsBasic16()
    {
        var rgb = AnsiColor.Rgb(255, 0, 0);
        Assert.IsTrue(rgb.TryDowngrade(AnsiColorLevel.Colors16, out var downgraded));
        Assert.AreEqual(AnsiColorKind.Basic16, downgraded.Kind);
    }

    [TestMethod]
    public void TryDowngrade_Indexed256_To16_ReturnsBasic16()
    {
        var c256 = AnsiColor.Indexed256(196);
        Assert.IsTrue(c256.TryDowngrade(AnsiColorLevel.Colors16, out var downgraded));
        Assert.AreEqual(AnsiColorKind.Basic16, downgraded.Kind);
    }
}

