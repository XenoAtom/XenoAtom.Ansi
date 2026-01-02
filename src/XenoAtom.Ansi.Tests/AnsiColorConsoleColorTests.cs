// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiColorConsoleColorTests
{
    [TestMethod]
    public void ConsoleColor_ImplicitConversion_MapsToAnsiBasic16()
    {
        AnsiColor c1 = ConsoleColor.DarkBlue;
        Assert.AreEqual(AnsiColorKind.Basic16, c1.Kind);
        Assert.AreEqual((byte)4, c1.Index);

        AnsiColor c2 = ConsoleColor.Red;
        Assert.AreEqual(AnsiColorKind.Basic16, c2.Kind);
        Assert.AreEqual((byte)9, c2.Index);

        AnsiColor c3 = ConsoleColor.Gray;
        Assert.AreEqual(AnsiColorKind.Basic16, c3.Kind);
        Assert.AreEqual((byte)7, c3.Index);

        AnsiColor c4 = ConsoleColor.DarkGray;
        Assert.AreEqual(AnsiColorKind.Basic16, c4.Kind);
        Assert.AreEqual((byte)8, c4.Index);
    }
}

