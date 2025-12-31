// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;

namespace XenoAtom.Ansi.Tokens;

/// <summary>
/// Convenience helpers for interpreting tokens as common terminal input events.
/// </summary>
public static class AnsiInputTokenExtensions
{
    /// <summary>
    /// Attempts to interpret a <see cref="CsiToken"/> as a cursor position report (CPR) (<c>ESC [ row ; col R</c>).
    /// </summary>
    public static bool TryGetCursorPositionReport(this CsiToken token, out AnsiCursorPosition position)
    {
        position = default;

        if (token.Final != 'R' || token.PrivateMarker is not null || token.Intermediates.Length != 0)
        {
            return false;
        }

        var parameters = token.Parameters;
        if (parameters.Length != 2)
        {
            return false;
        }

        var row = parameters[0];
        var col = parameters[1];
        if (row < 1 || col < 1)
        {
            return false;
        }

        position = new AnsiCursorPosition(row, col);
        return true;
    }

    /// <summary>
    /// Attempts to interpret a <see cref="CsiToken"/> as an xterm SGR mouse event (<c>CSI &lt; b ; x ; y M/m</c>).
    /// </summary>
    public static bool TryGetSgrMouseEvent(this CsiToken token, out AnsiMouseEvent mouseEvent)
    {
        mouseEvent = default;

        if (token.PrivateMarker != '<' || token.Intermediates.Length != 0)
        {
            return false;
        }

        var final = token.Final;
        if (final is not ('M' or 'm'))
        {
            return false;
        }

        var parameters = token.Parameters;
        if (parameters.Length != 3)
        {
            return false;
        }

        var cb = parameters[0];
        var x = parameters[1];
        var y = parameters[2];
        if (cb < 0 || x < 1 || y < 1)
        {
            return false;
        }

        var modifiers = AnsiKeyModifiers.None;
        if ((cb & 4) != 0) modifiers |= AnsiKeyModifiers.Shift;
        if ((cb & 8) != 0) modifiers |= AnsiKeyModifiers.Alt;
        if ((cb & 16) != 0) modifiers |= AnsiKeyModifiers.Control;

        var motion = (cb & 32) != 0;
        var wheel = (cb & 64) != 0;
        var buttonCode = cb & 3;

        if (wheel)
        {
            var delta = buttonCode switch
            {
                0 => 1,
                1 => -1,
                _ => 0,
            };

            mouseEvent = new AnsiMouseEvent(AnsiMouseAction.Wheel, x, y, AnsiMouseButton.None, delta, modifiers);
            return true;
        }

        if (final == 'm')
        {
            mouseEvent = new AnsiMouseEvent(AnsiMouseAction.Release, x, y, ToMouseButton(buttonCode), 0, modifiers);
            return true;
        }

        if (motion)
        {
            mouseEvent = new AnsiMouseEvent(AnsiMouseAction.Move, x, y, ToMouseButton(buttonCode), 0, modifiers);
            return true;
        }

        mouseEvent = new AnsiMouseEvent(AnsiMouseAction.Press, x, y, ToMouseButton(buttonCode), 0, modifiers);
        return true;
    }

    /// <summary>
    /// Attempts to interpret this token as a special key input (arrows, function keys, etc.).
    /// </summary>
    public static bool TryGetKeyEvent(this AnsiToken token, out AnsiKeyEvent keyEvent)
    {
        keyEvent = default;

        switch (token)
        {
            case CsiToken csi:
                return TryGetKeyEvent(csi, out keyEvent);
            case Ss3Token ss3:
                return TryGetKeyEvent(ss3, out keyEvent);
            case ControlToken control:
                return TryGetKeyEvent(control, out keyEvent);
            case TextToken text:
                return TryGetKeyEvent(text, out keyEvent);
            case UnknownEscapeToken unknown:
                // ESC key can be represented as a standalone ESC byte which, in final-chunk mode,
                // is flushed as an UnknownEscapeToken containing the raw ESC.
                if (unknown.Raw is "\x1b")
                {
                    keyEvent = new AnsiKeyEvent(AnsiKey.Escape);
                    return true;
                }
                return false;
            default:
                return false;
        }
    }

    /// <summary>
    /// Attempts to interpret a CSI token as a key event (Windows Console VT / xterm conventions).
    /// </summary>
    public static bool TryGetKeyEvent(this CsiToken token, out AnsiKeyEvent keyEvent)
    {
        keyEvent = default;

        if (token.PrivateMarker is not null || token.Intermediates.Length != 0)
        {
            return false;
        }

        var final = token.Final;
        if (final is 'A' or 'B' or 'C' or 'D' or 'H' or 'F')
        {
            var parameters = token.Parameters;
            var modifiers = AnsiKeyModifiers.None;
            if (parameters.Length == 0)
            {
                modifiers = AnsiKeyModifiers.None;
            }
            else if (parameters.Length >= 2 && parameters[0] == 1)
            {
                modifiers = DecodeXtermModifierValue(parameters[1]);
            }
            else
            {
                return false;
            }

            var key = final switch
            {
                'A' => AnsiKey.Up,
                'B' => AnsiKey.Down,
                'C' => AnsiKey.Right,
                'D' => AnsiKey.Left,
                'H' => AnsiKey.Home,
                'F' => AnsiKey.End,
                _ => AnsiKey.Unknown,
            };

            if (key == AnsiKey.Unknown)
            {
                return false;
            }

            keyEvent = new AnsiKeyEvent(key, modifiers);
            return true;
        }

        // BackTab (shift-tab) is commonly emitted as CSI Z.
        if (final == 'Z' && token.Parameters.Length == 0)
        {
            keyEvent = new AnsiKeyEvent(AnsiKey.BackTab);
            return true;
        }

        if (final == '~')
        {
            var parameters = token.Parameters;
            if (parameters.Length == 0)
            {
                return false;
            }

            var code = parameters[0];
            var key = code switch
            {
                2 => AnsiKey.Insert,
                3 => AnsiKey.Delete,
                5 => AnsiKey.PageUp,
                6 => AnsiKey.PageDown,
                15 => AnsiKey.F5,
                17 => AnsiKey.F6,
                18 => AnsiKey.F7,
                19 => AnsiKey.F8,
                20 => AnsiKey.F9,
                21 => AnsiKey.F10,
                23 => AnsiKey.F11,
                24 => AnsiKey.F12,
                _ => AnsiKey.Unknown,
            };

            if (key == AnsiKey.Unknown)
            {
                return false;
            }

            var modifiers = parameters.Length >= 2 ? DecodeXtermModifierValue(parameters[1]) : AnsiKeyModifiers.None;
            keyEvent = new AnsiKeyEvent(key, modifiers);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to interpret an SS3 token (<c>ESC O final</c>) as a key event (application mode arrows, F1–F4).
    /// </summary>
    public static bool TryGetKeyEvent(this Ss3Token token, out AnsiKeyEvent keyEvent)
    {
        keyEvent = default;

        var key = token.Final switch
        {
            'A' => AnsiKey.Up,
            'B' => AnsiKey.Down,
            'C' => AnsiKey.Right,
            'D' => AnsiKey.Left,
            'H' => AnsiKey.Home,
            'F' => AnsiKey.End,
            'P' => AnsiKey.F1,
            'Q' => AnsiKey.F2,
            'R' => AnsiKey.F3,
            'S' => AnsiKey.F4,
            _ => AnsiKey.Unknown,
        };

        if (key == AnsiKey.Unknown)
        {
            return false;
        }

        keyEvent = new AnsiKeyEvent(key);
        return true;
    }

    private static bool TryGetKeyEvent(ControlToken token, out AnsiKeyEvent keyEvent)
    {
        keyEvent = default;

        switch (token.Control)
        {
            case '\t':
                keyEvent = new AnsiKeyEvent(AnsiKey.Tab);
                return true;
            case '\r':
            case '\n':
                keyEvent = new AnsiKeyEvent(AnsiKey.Enter);
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetKeyEvent(TextToken token, out AnsiKeyEvent keyEvent)
    {
        keyEvent = default;

        // Backspace is commonly DEL (0x7F) on the input stream.
        if (token.Text.Length == 1 && token.Text[0] == '\x7f')
        {
            keyEvent = new AnsiKeyEvent(AnsiKey.Backspace);
            return true;
        }

        return false;
    }

    private static AnsiMouseButton ToMouseButton(int buttonCode) => buttonCode switch
    {
        0 => AnsiMouseButton.Left,
        1 => AnsiMouseButton.Middle,
        2 => AnsiMouseButton.Right,
        _ => AnsiMouseButton.None,
    };

    private static AnsiKeyModifiers DecodeXtermModifierValue(int m)
    {
        // 1 means no modifiers; others map as described above.
        return m switch
        {
            2 => AnsiKeyModifiers.Shift,
            3 => AnsiKeyModifiers.Alt,
            4 => AnsiKeyModifiers.Shift | AnsiKeyModifiers.Alt,
            5 => AnsiKeyModifiers.Control,
            6 => AnsiKeyModifiers.Shift | AnsiKeyModifiers.Control,
            7 => AnsiKeyModifiers.Alt | AnsiKeyModifiers.Control,
            8 => AnsiKeyModifiers.Shift | AnsiKeyModifiers.Alt | AnsiKeyModifiers.Control,
            _ => AnsiKeyModifiers.None,
        };
    }
}
