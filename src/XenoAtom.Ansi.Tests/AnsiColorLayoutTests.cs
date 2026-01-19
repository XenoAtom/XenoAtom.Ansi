// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiColorLayoutTests
{
    [TestMethod]
    public void SizeOf_IsSingleUInt()
    {
        Assert.AreEqual(4, Unsafe.SizeOf<AnsiColor>());
    }

    [TestMethod]
    public void Properties_AreStableAcrossKinds()
    {
        var defaultColor = AnsiColor.Default;
        Assert.AreEqual(AnsiColorKind.Default, defaultColor.Kind);
        Assert.AreEqual(0, defaultColor.Index);
        Assert.AreEqual(0, defaultColor.R);
        Assert.AreEqual(0, defaultColor.G);
        Assert.AreEqual(0, defaultColor.B);

        var basic = AnsiColor.Basic16(5);
        Assert.AreEqual(AnsiColorKind.Basic16, basic.Kind);
        Assert.AreEqual(5, basic.Index);
        Assert.AreEqual(0, basic.R);
        Assert.AreEqual(0, basic.G);
        Assert.AreEqual(0, basic.B);

        var indexed = AnsiColor.Indexed256(200);
        Assert.AreEqual(AnsiColorKind.Indexed256, indexed.Kind);
        Assert.AreEqual(200, indexed.Index);
        Assert.AreEqual(0, indexed.R);
        Assert.AreEqual(0, indexed.G);
        Assert.AreEqual(0, indexed.B);

        var rgb = AnsiColor.Rgb(1, 2, 3);
        Assert.AreEqual(AnsiColorKind.Rgb, rgb.Kind);
        Assert.AreEqual(0, rgb.Index);
        Assert.AreEqual(1, rgb.R);
        Assert.AreEqual(2, rgb.G);
        Assert.AreEqual(3, rgb.B);
    }
}

