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

    [TestMethod]
    public void AnsiPalettes_Basic16Rgb_HasKnownValues()
    {
        // Basic 16 palette is xterm-like defaults.
        Assert.AreEqual((byte)0, AnsiPalettes.GetBasic16Rgb(0).R);
        Assert.AreEqual((byte)0, AnsiPalettes.GetBasic16Rgb(0).G);
        Assert.AreEqual((byte)0, AnsiPalettes.GetBasic16Rgb(0).B);

        Assert.AreEqual((byte)255, AnsiPalettes.GetBasic16Rgb(15).R);
        Assert.AreEqual((byte)255, AnsiPalettes.GetBasic16Rgb(15).G);
        Assert.AreEqual((byte)255, AnsiPalettes.GetBasic16Rgb(15).B);
    }

    [TestMethod]
    public void AnsiPalettes_ThrowsOnOutOfRangeIndices()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AnsiPalettes.GetBasic16Rgb(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => AnsiPalettes.GetBasic16Rgb(16));
        Assert.Throws<ArgumentOutOfRangeException>(() => AnsiPalettes.GetXterm256Rgb(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => AnsiPalettes.GetXterm256Rgb(256));
    }
}

