// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;
using XenoAtom.Ansi.Tokens;

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiWriterTerminalControlTests
{
    [TestMethod]
    public void ScrollAndRegion_ParsesAsCsi()
    {
        var tokens = AnsiRoundTrip.EmitAndTokenize(w =>
        {
            w.SetScrollRegion(2, 5);
            w.ScrollUp(3);
            w.ScrollDown(4);
            w.ResetScrollRegion();
        });

        Assert.HasCount(4, tokens);

        var region = (CsiToken)tokens[0];
        Assert.AreEqual('r', region.Final);
        CollectionAssert.AreEqual(new[] { 2, 5 }, region.Parameters);

        var up = (CsiToken)tokens[1];
        Assert.AreEqual('S', up.Final);
        CollectionAssert.AreEqual(new[] { 3 }, up.Parameters);

        var down = (CsiToken)tokens[2];
        Assert.AreEqual('T', down.Final);
        CollectionAssert.AreEqual(new[] { 4 }, down.Parameters);

        var reset = (CsiToken)tokens[3];
        Assert.AreEqual('r', reset.Final);
        CollectionAssert.AreEqual(Array.Empty<int>(), reset.Parameters);
    }

    [TestMethod]
    public void InsertDeleteAndErase_ParsesAsCsi()
    {
        var tokens = AnsiRoundTrip.EmitAndTokenize(w =>
        {
            w.InsertLines(2);
            w.DeleteLines(3);
            w.InsertCharacters(4);
            w.DeleteCharacters(5);
            w.EraseCharacters(6);
            w.EraseScrollback();
        });

        Assert.HasCount(6, tokens);
        Assert.AreEqual('L', ((CsiToken)tokens[0]).Final);
        Assert.AreEqual('M', ((CsiToken)tokens[1]).Final);
        Assert.AreEqual('@', ((CsiToken)tokens[2]).Final);
        Assert.AreEqual('P', ((CsiToken)tokens[3]).Final);
        Assert.AreEqual('X', ((CsiToken)tokens[4]).Final);
        Assert.AreEqual('J', ((CsiToken)tokens[5]).Final);
        CollectionAssert.AreEqual(new[] { 3 }, ((CsiToken)tokens[5]).Parameters);
    }

    [TestMethod]
    public void CursorStyle_ParsesAsCsiWithIntermediate()
    {
        var csi = (CsiToken)AnsiRoundTrip.EmitAndTokenize(w => w.CursorStyle(AnsiCursorStyle.BlinkingUnderline)).Single();
        Assert.AreEqual('q', csi.Final);
        Assert.AreEqual(" ", csi.Intermediates);
        CollectionAssert.AreEqual(new[] { 3 }, csi.Parameters);
    }

    [TestMethod]
    public void SaveRestoreCursor_CsiVariants_ParseAsCsi()
    {
        var tokens = AnsiRoundTrip.EmitAndTokenize(w =>
        {
            w.SaveCursorPosition();
            w.RestoreCursorPosition();
        });

        Assert.HasCount(2, tokens);
        Assert.AreEqual('s', ((CsiToken)tokens[0]).Final);
        Assert.AreEqual('u', ((CsiToken)tokens[1]).Final);
    }

    [TestMethod]
    public void ModeControls_ParseAsCsiWithPrivateMarkerWhenDec()
    {
        var tokens = AnsiRoundTrip.EmitAndTokenize(w =>
        {
            w.SetMode(4, enabled: true);
            w.SetMode(4, enabled: false);
            w.PrivateMode(2004, enabled: true);
            w.PrivateMode(2004, enabled: false);
        });

        Assert.HasCount(4, tokens);

        var sm = (CsiToken)tokens[0];
        Assert.AreEqual('h', sm.Final);
        Assert.IsNull(sm.PrivateMarker);
        CollectionAssert.AreEqual(new[] { 4 }, sm.Parameters);

        var rm = (CsiToken)tokens[1];
        Assert.AreEqual('l', rm.Final);
        Assert.IsNull(rm.PrivateMarker);
        CollectionAssert.AreEqual(new[] { 4 }, rm.Parameters);

        var decset = (CsiToken)tokens[2];
        Assert.AreEqual('h', decset.Final);
        Assert.AreEqual('?', decset.PrivateMarker);
        CollectionAssert.AreEqual(new[] { 2004 }, decset.Parameters);

        var decrst = (CsiToken)tokens[3];
        Assert.AreEqual('l', decrst.Final);
        Assert.AreEqual('?', decrst.PrivateMarker);
        CollectionAssert.AreEqual(new[] { 2004 }, decrst.Parameters);
    }
}
