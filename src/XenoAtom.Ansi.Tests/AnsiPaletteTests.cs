// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiPaletteTests
{
    [TestMethod]
    public void AnsiColors_Basic16Indices_AreStable()
    {
        Assert.AreEqual(AnsiColor.Basic16(1), AnsiColors.Red);
        Assert.AreEqual(AnsiColor.Basic16(9), AnsiColors.BrightRed);
    }

    [TestMethod]
    public void AnsiPalettes_Xterm256Rgb_HasKnownValues()
    {
        // Index 16 is the start of the 6x6x6 color cube, and maps to (0,0,0).
        Assert.AreEqual((byte)0, AnsiPalettes.GetXterm256Rgb(16).R);
        Assert.AreEqual((byte)0, AnsiPalettes.GetXterm256Rgb(16).G);
        Assert.AreEqual((byte)0, AnsiPalettes.GetXterm256Rgb(16).B);

        // Index 231 is the end of the cube, and maps to (255,255,255).
        Assert.AreEqual((byte)255, AnsiPalettes.GetXterm256Rgb(231).R);
        Assert.AreEqual((byte)255, AnsiPalettes.GetXterm256Rgb(231).G);
        Assert.AreEqual((byte)255, AnsiPalettes.GetXterm256Rgb(231).B);
    }
}

