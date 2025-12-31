// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi.Tokens;

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiInputParsingTests
{
    [TestMethod]
    public void Ss3Tokenizer_ParsesAsSs3Token()
    {
        var token = AnsiRoundTrip.EmitAndTokenize(w => w.WriteSs3('A')).Single();
        Assert.IsInstanceOfType<Ss3Token>(token);

        var ss3 = (Ss3Token)token;
        Assert.AreEqual('A', ss3.Final);
        Assert.IsTrue(ss3.TryGetKeyEvent(out var keyEvent));
        Assert.AreEqual(AnsiKey.Up, keyEvent.Key);
        Assert.AreEqual(AnsiKeyModifiers.None, keyEvent.Modifiers);
    }

    [TestMethod]
    public void KeyEvent_F1_ParsesFromSs3()
    {
        var token = AnsiRoundTrip.EmitAndTokenize(w => w.WriteKeyEvent(new AnsiKeyEvent(AnsiKey.F1))).Single();
        Assert.IsInstanceOfType<Ss3Token>(token);
        Assert.IsTrue(token.TryGetKeyEvent(out var keyEvent));
        Assert.AreEqual(AnsiKey.F1, keyEvent.Key);
    }

    [TestMethod]
    public void KeyEvent_CtrlUp_ParsesFromCsi()
    {
        var token = AnsiRoundTrip.EmitAndTokenize(w => w.WriteKeyEvent(new AnsiKeyEvent(AnsiKey.Up, AnsiKeyModifiers.Control))).Single();
        Assert.IsInstanceOfType<CsiToken>(token);

        var csi = (CsiToken)token;
        Assert.AreEqual('A', csi.Final);
        CollectionAssert.AreEqual(new[] { 1, 5 }, csi.Parameters);

        Assert.IsTrue(token.TryGetKeyEvent(out var keyEvent));
        Assert.AreEqual(AnsiKey.Up, keyEvent.Key);
        Assert.AreEqual(AnsiKeyModifiers.Control, keyEvent.Modifiers);
    }

    [TestMethod]
    public void KeyEvent_Escape_ParsesFromUnknownEscapeToken()
    {
        var token = AnsiRoundTrip.EmitAndTokenize(w => w.WriteKeyEvent(new AnsiKeyEvent(AnsiKey.Escape))).Single();
        Assert.IsInstanceOfType<UnknownEscapeToken>(token);
        Assert.IsTrue(token.TryGetKeyEvent(out var keyEvent));
        Assert.AreEqual(AnsiKey.Escape, keyEvent.Key);
    }

    [TestMethod]
    public void KeyEvent_Backspace_ParsesFromDelText()
    {
        var token = AnsiRoundTrip.EmitAndTokenize(w => w.WriteKeyEvent(new AnsiKeyEvent(AnsiKey.Backspace))).Single();
        Assert.IsInstanceOfType<TextToken>(token);
        Assert.IsTrue(token.TryGetKeyEvent(out var keyEvent));
        Assert.AreEqual(AnsiKey.Backspace, keyEvent.Key);
    }

    [TestMethod]
    public void CursorPositionReport_ParsesFromCsi()
    {
        var token = AnsiRoundTrip.EmitAndTokenize(w => w.WriteCursorPositionReport(12, 34)).Single();
        Assert.IsInstanceOfType<CsiToken>(token);

        var csi = (CsiToken)token;
        Assert.IsTrue(csi.TryGetCursorPositionReport(out var position));
        Assert.AreEqual(12, position.Row);
        Assert.AreEqual(34, position.Column);
    }

    [TestMethod]
    public void SgrMouseEvent_ParsesPressReleaseWheel()
    {
        var tokens = AnsiRoundTrip.EmitAndTokenize(w =>
        {
            w.WriteSgrMouseEvent(new AnsiMouseEvent(AnsiMouseAction.Press, X: 10, Y: 20, Button: AnsiMouseButton.Left, Modifiers: AnsiKeyModifiers.Control));
            w.WriteSgrMouseEvent(new AnsiMouseEvent(AnsiMouseAction.Release, X: 10, Y: 20, Button: AnsiMouseButton.Left, Modifiers: AnsiKeyModifiers.Control));
            w.WriteSgrMouseEvent(new AnsiMouseEvent(AnsiMouseAction.Wheel, X: 10, Y: 20, WheelDelta: 1));
        });

        Assert.HasCount(3, tokens);

        Assert.IsTrue(((CsiToken)tokens[0]).TryGetSgrMouseEvent(out var press));
        Assert.AreEqual(AnsiMouseAction.Press, press.Action);
        Assert.AreEqual(AnsiMouseButton.Left, press.Button);
        Assert.AreEqual(10, press.X);
        Assert.AreEqual(20, press.Y);
        Assert.AreEqual(AnsiKeyModifiers.Control, press.Modifiers);

        Assert.IsTrue(((CsiToken)tokens[1]).TryGetSgrMouseEvent(out var release));
        Assert.AreEqual(AnsiMouseAction.Release, release.Action);
        Assert.AreEqual(AnsiMouseButton.Left, release.Button);

        Assert.IsTrue(((CsiToken)tokens[2]).TryGetSgrMouseEvent(out var wheel));
        Assert.AreEqual(AnsiMouseAction.Wheel, wheel.Action);
        Assert.AreEqual(1, wheel.WheelDelta);
    }
}
