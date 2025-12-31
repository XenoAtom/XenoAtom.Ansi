// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi.Tokens;

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiWriterWindowsVirtualTerminalTests
{
    [TestMethod]
    public void WindowTitle_ParsesAsOsc()
    {
        var osc = (OscToken)AnsiRoundTrip.EmitAndTokenize(w => w.WindowTitle("Hello")).Single();
        Assert.AreEqual(2, osc.Code);
        Assert.AreEqual("Hello", osc.Data);
    }

    [TestMethod]
    public void IconAndWindowTitle_ParsesAsOsc()
    {
        var osc = (OscToken)AnsiRoundTrip.EmitAndTokenize(w => w.IconAndWindowTitle("Hello")).Single();
        Assert.AreEqual(0, osc.Code);
        Assert.AreEqual("Hello", osc.Data);
    }

    [TestMethod]
    public void SetPaletteColor_ParsesAsOsc()
    {
        var osc = (OscToken)AnsiRoundTrip.EmitAndTokenize(w => w.SetPaletteColor(1, 0x01, 0x24, 0x86)).Single();
        Assert.AreEqual(4, osc.Code);
        Assert.AreEqual("1;rgb:01/24/86", osc.Data);
    }

    [TestMethod]
    public void TabsAndCharset_ParsesAsEscAndCsi()
    {
        var tokens = AnsiRoundTrip.EmitAndTokenize(w =>
        {
            w.HorizontalTabSet();
            w.CursorForwardTab(2);
            w.CursorBackTab(3);
            w.ClearTabStop();
            w.ClearAllTabStops();
            w.EnterLineDrawingMode();
            w.ExitLineDrawingMode();
            w.ReverseIndex();
        });

        Assert.HasCount(8, tokens);

        var hts = (EscToken)tokens[0];
        Assert.AreEqual("", hts.Intermediates);
        Assert.AreEqual('H', hts.Final);

        var cht = (CsiToken)tokens[1];
        Assert.AreEqual('I', cht.Final);
        CollectionAssert.AreEqual(new[] { 2 }, cht.Parameters);

        var cbt = (CsiToken)tokens[2];
        Assert.AreEqual('Z', cbt.Final);
        CollectionAssert.AreEqual(new[] { 3 }, cbt.Parameters);

        var tbc0 = (CsiToken)tokens[3];
        Assert.AreEqual('g', tbc0.Final);
        CollectionAssert.AreEqual(new[] { 0 }, tbc0.Parameters);

        var tbc3 = (CsiToken)tokens[4];
        Assert.AreEqual('g', tbc3.Final);
        CollectionAssert.AreEqual(new[] { 3 }, tbc3.Parameters);

        var enter = (EscToken)tokens[5];
        Assert.AreEqual("(", enter.Intermediates);
        Assert.AreEqual('0', enter.Final);

        var exit = (EscToken)tokens[6];
        Assert.AreEqual("(", exit.Intermediates);
        Assert.AreEqual('B', exit.Final);

        var ri = (EscToken)tokens[7];
        Assert.AreEqual("", ri.Intermediates);
        Assert.AreEqual('M', ri.Final);
    }

    [TestMethod]
    public void ModeChangesAndQueries_ParseAsEscAndCsi()
    {
        var tokens = AnsiRoundTrip.EmitAndTokenize(w =>
        {
            w.KeypadApplicationMode();
            w.KeypadNumericMode();
            w.CursorKeysApplicationMode(enabled: true);
            w.CursorKeysApplicationMode(enabled: false);
            w.CursorBlinking(enabled: true);
            w.CursorBlinking(enabled: false);
            w.Columns132(enabled: true);
            w.Columns132(enabled: false);
            w.RequestCursorPosition();
            w.RequestDeviceAttributes();
        });

        Assert.HasCount(10, tokens);

        Assert.AreEqual('=', ((EscToken)tokens[0]).Final);
        Assert.AreEqual('>', ((EscToken)tokens[1]).Final);

        var decckmSet = (CsiToken)tokens[2];
        Assert.AreEqual('h', decckmSet.Final);
        Assert.AreEqual('?', decckmSet.PrivateMarker);
        CollectionAssert.AreEqual(new[] { 1 }, decckmSet.Parameters);

        var decckmRst = (CsiToken)tokens[3];
        Assert.AreEqual('l', decckmRst.Final);
        Assert.AreEqual('?', decckmRst.PrivateMarker);
        CollectionAssert.AreEqual(new[] { 1 }, decckmRst.Parameters);

        var blinkSet = (CsiToken)tokens[4];
        Assert.AreEqual('h', blinkSet.Final);
        Assert.AreEqual('?', blinkSet.PrivateMarker);
        CollectionAssert.AreEqual(new[] { 12 }, blinkSet.Parameters);

        var blinkRst = (CsiToken)tokens[5];
        Assert.AreEqual('l', blinkRst.Final);
        Assert.AreEqual('?', blinkRst.PrivateMarker);
        CollectionAssert.AreEqual(new[] { 12 }, blinkRst.Parameters);

        var colsSet = (CsiToken)tokens[6];
        Assert.AreEqual('h', colsSet.Final);
        Assert.AreEqual('?', colsSet.PrivateMarker);
        CollectionAssert.AreEqual(new[] { 3 }, colsSet.Parameters);

        var colsRst = (CsiToken)tokens[7];
        Assert.AreEqual('l', colsRst.Final);
        Assert.AreEqual('?', colsRst.PrivateMarker);
        CollectionAssert.AreEqual(new[] { 3 }, colsRst.Parameters);

        var cpr = (CsiToken)tokens[8];
        Assert.AreEqual('n', cpr.Final);
        CollectionAssert.AreEqual(new[] { 6 }, cpr.Parameters);

        var da = (CsiToken)tokens[9];
        Assert.AreEqual('c', da.Final);
        CollectionAssert.AreEqual(Array.Empty<int>(), da.Parameters);
    }
}
