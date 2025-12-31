// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

public partial class AnsiWriter
{
    /// <summary>
    /// Writes a cursor position report (CPR) sequence (<c>ESC [ row ; col R</c>).
    /// </summary>
    /// <remarks>
    /// This method is intended for test and host scenarios; it always writes the sequence regardless of <see cref="Capabilities"/>.
    /// </remarks>
    /// <param name="row">The 1-based row.</param>
    /// <param name="column">The 1-based column.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter WriteCursorPositionReport(int row, int column)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(row, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(column, 1);

        Write("\x1b[");
        WriteInt(row);
        WriteChar(';');
        WriteInt(column);
        WriteChar('R');
        return this;
    }

    /// <summary>
    /// Writes an SS3 sequence (<c>ESC O final</c>).
    /// </summary>
    /// <remarks>
    /// SS3 is commonly used by terminals to encode input keys (e.g. arrow keys in application mode and F1–F4).
    /// This method is intended for test and host scenarios; it always writes the sequence regardless of <see cref="Capabilities"/>.
    /// </remarks>
    /// <param name="final">The final byte.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter WriteSs3(char final)
    {
        Write("\x1bO");
        WriteChar(final);
        return this;
    }

    /// <summary>
    /// Writes an xterm SGR mouse event (<c>CSI &lt; b ; x ; y M/m</c>).
    /// </summary>
    /// <remarks>
    /// This method is intended for test and host scenarios; it always writes the sequence regardless of <see cref="Capabilities"/>.
    /// </remarks>
    /// <param name="mouseEvent">The mouse event.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter WriteSgrMouseEvent(AnsiMouseEvent mouseEvent)
    {
        if (mouseEvent.X < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(mouseEvent), mouseEvent.X, "X must be 1-based.");
        }

        if (mouseEvent.Y < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(mouseEvent), mouseEvent.Y, "Y must be 1-based.");
        }

        var cb = EncodeMouseCb(mouseEvent);
        var final = mouseEvent.Action == AnsiMouseAction.Release ? 'm' : 'M';

        Write("\x1b[<");
        WriteInt(cb);
        WriteChar(';');
        WriteInt(mouseEvent.X);
        WriteChar(';');
        WriteInt(mouseEvent.Y);
        WriteChar(final);
        return this;
    }

    /// <summary>
    /// Writes a common key input sequence (Windows Console VT / xterm conventions).
    /// </summary>
    /// <remarks>
    /// This method is intended for test and host scenarios; it always writes the sequence regardless of <see cref="Capabilities"/>.
    /// It does not attempt to cover every terminal key protocol.
    /// </remarks>
    /// <param name="keyEvent">The key event.</param>
    /// <param name="applicationCursorKeysMode">
    /// When <see langword="true"/>, arrow/Home/End without modifiers are emitted in SS3 form (<c>ESC O</c>...).
    /// </param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter WriteKeyEvent(AnsiKeyEvent keyEvent, bool applicationCursorKeysMode = false)
    {
        var modifiers = keyEvent.Modifiers;
        var hasModifiers = modifiers != AnsiKeyModifiers.None;

        switch (keyEvent.Key)
        {
            case AnsiKey.Escape:
                WriteChar('\x1b');
                return this;
            case AnsiKey.Enter:
                WriteChar('\r');
                return this;
            case AnsiKey.Tab:
                WriteChar('\t');
                return this;
            case AnsiKey.BackTab:
                Write("\x1b[Z");
                return this;
            case AnsiKey.Backspace:
                WriteChar('\x7f');
                return this;
        }

        if (keyEvent.Key is AnsiKey.Up or AnsiKey.Down or AnsiKey.Left or AnsiKey.Right or AnsiKey.Home or AnsiKey.End)
        {
            var final = keyEvent.Key switch
            {
                AnsiKey.Up => 'A',
                AnsiKey.Down => 'B',
                AnsiKey.Right => 'C',
                AnsiKey.Left => 'D',
                AnsiKey.Home => 'H',
                AnsiKey.End => 'F',
                _ => '\0',
            };

            if (final == '\0')
            {
                return this;
            }

            if (hasModifiers)
            {
                // ESC [ 1 ; <m> <final>
                Write("\x1b[1;");
                WriteInt(EncodeXtermModifierValue(modifiers));
                WriteChar(final);
                return this;
            }

            if (applicationCursorKeysMode)
            {
                return WriteSs3(final);
            }

            Write("\x1b[");
            WriteChar(final);
            return this;
        }

        if (keyEvent.Key is AnsiKey.F1 or AnsiKey.F2 or AnsiKey.F3 or AnsiKey.F4)
        {
            var final = keyEvent.Key switch
            {
                AnsiKey.F1 => 'P',
                AnsiKey.F2 => 'Q',
                AnsiKey.F3 => 'R',
                AnsiKey.F4 => 'S',
                _ => '\0',
            };

            if (final != '\0')
            {
                return WriteSs3(final);
            }

            return this;
        }

        if (TryGetCsiTildeCode(keyEvent.Key, out var tildeCode))
        {
            Write("\x1b[");
            WriteInt(tildeCode);
            if (hasModifiers)
            {
                WriteChar(';');
                WriteInt(EncodeXtermModifierValue(modifiers));
            }
            WriteChar('~');
        }

        return this;
    }

    private static bool TryGetCsiTildeCode(AnsiKey key, out int code)
    {
        code = key switch
        {
            AnsiKey.Insert => 2,
            AnsiKey.Delete => 3,
            AnsiKey.PageUp => 5,
            AnsiKey.PageDown => 6,
            AnsiKey.F5 => 15,
            AnsiKey.F6 => 17,
            AnsiKey.F7 => 18,
            AnsiKey.F8 => 19,
            AnsiKey.F9 => 20,
            AnsiKey.F10 => 21,
            AnsiKey.F11 => 23,
            AnsiKey.F12 => 24,
            _ => 0,
        };
        return code != 0;
    }

    private static int EncodeXtermModifierValue(AnsiKeyModifiers modifiers)
    {
        // xterm modifier encoding:
        //   1 none, 2 shift, 3 alt, 4 shift+alt, 5 ctrl, 6 shift+ctrl, 7 alt+ctrl, 8 shift+alt+ctrl
        var shift = (modifiers & AnsiKeyModifiers.Shift) != 0;
        var alt = (modifiers & AnsiKeyModifiers.Alt) != 0;
        var ctrl = (modifiers & AnsiKeyModifiers.Control) != 0;

        if (!shift && !alt && !ctrl) return 1;
        if (shift && !alt && !ctrl) return 2;
        if (!shift && alt && !ctrl) return 3;
        if (shift && alt && !ctrl) return 4;
        if (!shift && !alt && ctrl) return 5;
        if (shift && !alt && ctrl) return 6;
        if (!shift && alt && ctrl) return 7;
        return 8;
    }

    private static int EncodeMouseCb(AnsiMouseEvent mouseEvent)
    {
        var cb = 0;
        if ((mouseEvent.Modifiers & AnsiKeyModifiers.Shift) != 0) cb |= 4;
        if ((mouseEvent.Modifiers & AnsiKeyModifiers.Alt) != 0) cb |= 8;
        if ((mouseEvent.Modifiers & AnsiKeyModifiers.Control) != 0) cb |= 16;

        switch (mouseEvent.Action)
        {
            case AnsiMouseAction.Move:
                cb |= 32;
                cb |= mouseEvent.Button switch
                {
                    AnsiMouseButton.Left => 0,
                    AnsiMouseButton.Middle => 1,
                    AnsiMouseButton.Right => 2,
                    _ => 3,
                };
                return cb;
            case AnsiMouseAction.Wheel:
                cb |= 64;
                cb |= mouseEvent.WheelDelta >= 0 ? 0 : 1;
                return cb;
            case AnsiMouseAction.Release:
            case AnsiMouseAction.Press:
            default:
                cb |= mouseEvent.Button switch
                {
                    AnsiMouseButton.Left => 0,
                    AnsiMouseButton.Middle => 1,
                    AnsiMouseButton.Right => 2,
                    _ => 3,
                };
                return cb;
        }
    }
}

