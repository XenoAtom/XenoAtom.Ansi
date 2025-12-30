// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using System.Globalization;
using XenoAtom.Ansi.Internal;

namespace XenoAtom.Ansi;

/// <summary>
/// Emits ANSI/VT escape sequences to a character sink.
/// </summary>
/// <remarks>
/// Terminology (common in ECMA-48 / ISO/IEC 6429 and xterm documentation):
/// <list type="bullet">
/// <item><description><b>ESC</b>: the escape control character (U+001B, <c>\x1b</c>)</description></item>
/// <item><description><b>CSI</b>: Control Sequence Introducer, typically <c>ESC [</c></description></item>
/// <item><description><b>SGR</b>: Select Graphic Rendition, a CSI sequence whose final byte is <c>m</c></description></item>
/// <item><description><b>OSC</b>: Operating System Command, introduced by <c>ESC ]</c></description></item>
/// </list>
/// This library is not a terminal emulator; it only emits the sequences required by rich-output renderers.
/// </remarks>
public class AnsiWriter
{
    private readonly IBufferWriter<char>? _bufferWriter;
    private readonly TextWriter? _textWriter;
    private readonly List<int> _codes;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnsiWriter"/> class that writes to an <see cref="IBufferWriter{T}"/>.
    /// </summary>
    /// <param name="bufferWriter">The output sink.</param>
    /// <param name="capabilities">Output capability knobs.</param>
    public AnsiWriter(IBufferWriter<char> bufferWriter, AnsiCapabilities? capabilities = null)
    {
        _bufferWriter = bufferWriter ?? throw new ArgumentNullException(nameof(bufferWriter));
        _textWriter = null;
        _codes = new List<int>(32);
        Capabilities = capabilities ?? AnsiCapabilities.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnsiWriter"/> class that writes to a <see cref="TextWriter"/>.
    /// </summary>
    /// <param name="textWriter">The output sink.</param>
    /// <param name="capabilities">Output capability knobs.</param>
    public AnsiWriter(TextWriter textWriter, AnsiCapabilities? capabilities = null)
    {
        _bufferWriter = null;
        _textWriter = textWriter ?? throw new ArgumentNullException(nameof(textWriter));
        _codes = new List<int>(32);
        Capabilities = capabilities ?? AnsiCapabilities.Default;
    }

    /// <summary>
    /// Gets the capabilities used by this writer.
    /// </summary>
    public AnsiCapabilities Capabilities { get; }

    /// <summary>
    /// Writes the specified text verbatim.
    /// </summary>
    /// <param name="text">The text to write.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter Write(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return this;
        }

        if (_textWriter is not null)
        {
            _textWriter.Write(text);
            return this;
        }

        if (_bufferWriter is null)
        {
            throw new InvalidOperationException("AnsiWriter was not initialized with an output.");
        }

        var span = _bufferWriter.GetSpan(text.Length);
        text.CopyTo(span);
        _bufferWriter.Advance(text.Length);
        return this;
    }

    /// <summary>
    /// Writes the specified text verbatim.
    /// </summary>
    /// <param name="text">The text to write.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter Write(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return this;
        }

        return Write(text.AsSpan());
    }

    private void WriteChar(char ch)
    {
        if (_textWriter is not null)
        {
            _textWriter.Write(ch);
            return;
        }

        if (_bufferWriter is null)
        {
            throw new InvalidOperationException("AnsiWriter was not initialized with an output.");
        }

        var span = _bufferWriter.GetSpan(1);
        span[0] = ch;
        _bufferWriter.Advance(1);
    }

    private void WriteInt(int value)
    {
        if (_textWriter is not null)
        {
            Span<char> buffer = stackalloc char[11];
            if (!value.TryFormat(buffer, out var charsWritten, provider: CultureInfo.InvariantCulture))
            {
                throw new InvalidOperationException("Failed to format an integer.");
            }

            _textWriter.Write(buffer[..charsWritten]);
            return;
        }

        if (_bufferWriter is null)
        {
            throw new InvalidOperationException("AnsiWriter was not initialized with an output.");
        }

        var span = _bufferWriter.GetSpan(11);
        if (!value.TryFormat(span, out var written))
        {
            throw new InvalidOperationException("Failed to format an integer.");
        }

        _bufferWriter.Advance(written);
    }

    /// <summary>
    /// Emits SGR reset (<c>ESC [ 0 m</c>).
    /// </summary>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter Reset()
    {
        WriteSgr("0");
        return this;
    }

    /// <summary>
    /// Emits SGR reset (<c>ESC [ 0 m</c>).
    /// </summary>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter ResetStyle() => Reset();

    /// <summary>
    /// Emits an SGR sequence that sets the foreground color.
    /// </summary>
    /// <param name="color">The color to set.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter Foreground(AnsiColor color)
    {
        if (!Capabilities.AnsiEnabled || Capabilities.ColorLevel == AnsiColorLevel.None)
        {
            return this;
        }

        WriteColorSgr(isForeground: true, color);
        return this;
    }

    /// <summary>
    /// Emits an SGR sequence that sets the background color.
    /// </summary>
    /// <param name="color">The color to set.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter Background(AnsiColor color)
    {
        if (!Capabilities.AnsiEnabled || Capabilities.ColorLevel == AnsiColorLevel.None)
        {
            return this;
        }

        WriteColorSgr(isForeground: false, color);
        return this;
    }

    /// <summary>
    /// Emits SGR to apply the specified style from the default style.
    /// </summary>
    /// <param name="style">The style to apply.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter Style(AnsiStyle style) => WriteStyleTransition(AnsiStyle.Default, style.ResolveMissingFrom(AnsiStyle.Default));

    /// <summary>
    /// Emits a minimal SGR sequence that transitions from one style to another.
    /// </summary>
    /// <param name="from">The current style.</param>
    /// <param name="to">The desired style.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter WriteStyleTransition(AnsiStyle from, AnsiStyle to) => WriteStyleTransition(from, to, Capabilities);

    /// <summary>
    /// Emits a minimal SGR sequence that transitions from one style to another using the specified capabilities.
    /// </summary>
    /// <param name="from">The current style.</param>
    /// <param name="to">The desired style.</param>
    /// <param name="capabilities">Capability knobs influencing emitted output.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter WriteStyleTransition(AnsiStyle from, AnsiStyle to, AnsiCapabilities capabilities)
    {
        if (!capabilities.AnsiEnabled)
        {
            return this;
        }

        var fromResolved = ResolveState(from, AnsiStyle.Default);
        var toResolved = ResolveState(to, fromResolved);

        _codes.Clear();

        if (capabilities.SafeMode)
        {
            _codes.Add(0);
            AddDecorationEnableCodes(toResolved.Decorations, _codes);
            if (toResolved.Foreground is { } fg)
            {
                AddColorCodes(isForeground: true, fg, capabilities, _codes);
            }
            if (toResolved.Background is { } bg)
            {
                AddColorCodes(isForeground: false, bg, capabilities, _codes);
            }

            WriteCsiSgrCodes(_codes);
            return this;
        }

        BuildSgrTransitionCodes(fromResolved, toResolved, capabilities, _codes);
        if (_codes.Count == 0)
        {
            return this;
        }

        WriteCsiSgrCodes(_codes);
        return this;
    }

    /// <summary>
    /// Emits SGR codes to enable or disable decoration flags.
    /// </summary>
    /// <param name="decorations">The decoration flag(s) to modify.</param>
    /// <param name="enabled"><see langword="true"/> to enable; <see langword="false"/> to disable.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    private AnsiWriter SetDecorationsCore(AnsiDecorations decorations, bool enabled)
    {
        if (!Capabilities.AnsiEnabled)
        {
            return this;
        }

        _codes.Clear();
        if (enabled)
        {
            AddDecorationEnableCodes(decorations, _codes);
        }
        else
        {
            AddDecorationDisableCodes(decorations, _codes);
        }

        if (_codes.Count > 0)
        {
            WriteCsiSgrCodes(_codes);
        }

        return this;
    }

    /// <summary>
    /// Enables the specified decoration flags (SGR).
    /// </summary>
    /// <param name="decorations">The decorations to enable.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter Decorate(AnsiDecorations decorations) => SetDecorationsCore(decorations, enabled: true);

    /// <summary>
    /// Disables the specified decoration flags (SGR).
    /// </summary>
    /// <param name="decorations">The decorations to disable.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter Undecorate(AnsiDecorations decorations) => SetDecorationsCore(decorations, enabled: false);

    /// <summary>
    /// Emits CSI cursor up (<c>ESC [ n A</c>).
    /// </summary>
    /// <param name="n">Number of cells; values &lt;= 0 are treated as 1 by most terminals.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter CursorUp(int n = 1)
    {
        WriteCsiWithInt(n, 'A');
        return this;
    }

    /// <summary>
    /// Emits CSI cursor down (<c>ESC [ n B</c>).
    /// </summary>
    /// <param name="n">Number of cells; values &lt;= 0 are treated as 1 by most terminals.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter CursorDown(int n = 1)
    {
        WriteCsiWithInt(n, 'B');
        return this;
    }

    /// <summary>
    /// Emits CSI cursor forward (<c>ESC [ n C</c>).
    /// </summary>
    /// <param name="n">Number of cells; values &lt;= 0 are treated as 1 by most terminals.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter CursorForward(int n = 1)
    {
        WriteCsiWithInt(n, 'C');
        return this;
    }

    /// <summary>
    /// Emits CSI cursor back (<c>ESC [ n D</c>).
    /// </summary>
    /// <param name="n">Number of cells; values &lt;= 0 are treated as 1 by most terminals.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter CursorBack(int n = 1)
    {
        WriteCsiWithInt(n, 'D');
        return this;
    }

    /// <summary>
    /// Emits CSI cursor position (<c>ESC [ row ; col H</c>), 1-based.
    /// </summary>
    /// <param name="row">The 1-based row.</param>
    /// <param name="col">The 1-based column.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter CursorPosition(int row, int col)
    {
        if (!Capabilities.AnsiEnabled)
        {
            return this;
        }

        if (row < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(row), row, "Row must be 1-based.");
        }

        if (col < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(col), col, "Column must be 1-based.");
        }

        Write("\x1b[");
        WriteInt(row);
        WriteChar(';');
        WriteInt(col);
        WriteChar('H');
        return this;
    }

    /// <summary>
    /// Emits CSI cursor position (<c>ESC [ row ; col H</c>), 1-based.
    /// </summary>
    /// <param name="row">The 1-based row.</param>
    /// <param name="col">The 1-based column.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter MoveTo(int row, int col) => CursorPosition(row, col);

    /// <summary>
    /// Emits CSI cursor up (<c>ESC [ n A</c>).
    /// </summary>
    /// <param name="n">Number of cells.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter Up(int n = 1) => CursorUp(n);

    /// <summary>
    /// Emits CSI cursor down (<c>ESC [ n B</c>).
    /// </summary>
    /// <param name="n">Number of cells.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter Down(int n = 1) => CursorDown(n);

    /// <summary>
    /// Emits CSI cursor forward (<c>ESC [ n C</c>).
    /// </summary>
    /// <param name="n">Number of cells.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter Forward(int n = 1) => CursorForward(n);

    /// <summary>
    /// Emits CSI cursor back (<c>ESC [ n D</c>).
    /// </summary>
    /// <param name="n">Number of cells.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter Back(int n = 1) => CursorBack(n);

    /// <summary>
    /// Emits the common DEC save cursor sequence (<c>ESC 7</c>).
    /// </summary>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter SaveCursor()
    {
        if (!Capabilities.AnsiEnabled)
        {
            return this;
        }

        Write("\u001b7");
        return this;
    }

    /// <summary>
    /// Emits the common DEC restore cursor sequence (<c>ESC 8</c>).
    /// </summary>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter RestoreCursor()
    {
        if (!Capabilities.AnsiEnabled)
        {
            return this;
        }

        Write("\u001b8");
        return this;
    }

    /// <summary>
    /// Emits CSI erase in line (<c>ESC [ n K</c>).
    /// </summary>
    /// <param name="mode">Erase mode (commonly 0, 1, 2).</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter EraseInLine(int mode = 0)
    {
        WriteCsiWithInt(mode, 'K', allowZeroToOmit: true);
        return this;
    }

    /// <summary>
    /// Emits CSI erase in display (<c>ESC [ n J</c>).
    /// </summary>
    /// <param name="mode">Erase mode (commonly 0, 1, 2, 3).</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter EraseInDisplay(int mode = 0)
    {
        WriteCsiWithInt(mode, 'J', allowZeroToOmit: true);
        return this;
    }

    /// <summary>
    /// Emits CSI erase in line (<c>ESC [ n K</c>).
    /// </summary>
    /// <param name="mode">Erase mode.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter EraseLine(int mode = 0) => EraseInLine(mode);

    /// <summary>
    /// Emits CSI erase in display (<c>ESC [ n J</c>).
    /// </summary>
    /// <param name="mode">Erase mode.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter EraseDisplay(int mode = 0) => EraseInDisplay(mode);

    /// <summary>
    /// Emits the DEC private mode sequence to show or hide the cursor (<c>ESC [ ? 25 h</c> / <c>ESC [ ? 25 l</c>).
    /// </summary>
    /// <param name="visible"><see langword="true"/> to show; <see langword="false"/> to hide.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter ShowCursor(bool visible)
    {
        if (!Capabilities.AnsiEnabled)
        {
            return this;
        }

        Write(visible ? "\x1b[?25h" : "\x1b[?25l");
        return this;
    }

    /// <summary>
    /// Shows or hides the cursor (<c>ESC [ ? 25 h</c> / <c>ESC [ ? 25 l</c>).
    /// </summary>
    /// <param name="visible"><see langword="true"/> to show; <see langword="false"/> to hide.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter CursorVisible(bool visible) => ShowCursor(visible);

    /// <summary>
    /// Emits the DEC private mode sequence to enter the alternate screen buffer (<c>ESC [ ? 1049 h</c>).
    /// </summary>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter EnterAlternateScreen()
    {
        if (!Capabilities.AnsiEnabled)
        {
            return this;
        }

        Write("\x1b[?1049h");
        return this;
    }

    /// <summary>
    /// Emits the DEC private mode sequence to leave the alternate screen buffer (<c>ESC [ ? 1049 l</c>).
    /// </summary>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter LeaveAlternateScreen()
    {
        if (!Capabilities.AnsiEnabled)
        {
            return this;
        }

        Write("\x1b[?1049l");
        return this;
    }

    /// <summary>
    /// Enters or leaves the alternate screen buffer (<c>ESC [ ? 1049 h</c> / <c>ESC [ ? 1049 l</c>).
    /// </summary>
    /// <param name="enabled"><see langword="true"/> to enter; <see langword="false"/> to leave.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter AlternateScreen(bool enabled) => enabled ? EnterAlternateScreen() : LeaveAlternateScreen();

    /// <summary>
    /// Emits a "soft reset" sequence (<c>ESC [ ! p</c>).
    /// </summary>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter SoftReset()
    {
        if (!Capabilities.AnsiEnabled)
        {
            return this;
        }

        Write("\x1b[!p");
        return this;
    }

    /// <summary>
    /// Begins an OSC 8 hyperlink (<c>ESC ] 8 ; params ; uri ST</c>).
    /// </summary>
    /// <param name="uri">The hyperlink target URI.</param>
    /// <param name="id">Optional id parameter (xterm convention: <c>id=...</c>).</param>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter BeginLink(string uri, string? id = null)
    {
        if (!Capabilities.AnsiEnabled || !Capabilities.SupportsOsc8)
        {
            return this;
        }

        if (uri is null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        Write("\x1b]8;");
        if (!string.IsNullOrEmpty(id))
        {
            Write("id=");
            Write(id);
        }

        Write(";");
        Write(uri);
        WriteOscTerminator(Capabilities);
        return this;
    }

    /// <summary>
    /// Ends the current OSC 8 hyperlink (<c>ESC ] 8 ; ; ST</c>).
    /// </summary>
    /// <returns>This writer, for fluent chaining.</returns>
    public AnsiWriter EndLink()
    {
        if (!Capabilities.AnsiEnabled || !Capabilities.SupportsOsc8)
        {
            return this;
        }

        Write("\x1b]8;;");
        WriteOscTerminator(Capabilities);
        return this;
    }

    private void WriteOscTerminator(AnsiCapabilities capabilities)
    {
        switch (capabilities.OscTermination)
        {
            case AnsiOscTermination.Bell:
                Write("\x07");
                break;
            case AnsiOscTermination.StringTerminator:
                Write("\x1b\\");
                break;
            default:
                Write("\x1b\\");
                break;
        }
    }

    private void WriteCsiWithInt(int n, char final, bool allowZeroToOmit = false)
    {
        if (!Capabilities.AnsiEnabled)
        {
            return;
        }

        if (n < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(n), n, "Value must be non-negative.");
        }

        if (allowZeroToOmit && n == 0)
        {
            Write("\x1b[");
            WriteChar(final);
            return;
        }

        Write("\x1b[");
        WriteInt(n == 0 ? 1 : n);
        WriteChar(final);
    }

    private void WriteSgr(string parameters)
    {
        if (!Capabilities.AnsiEnabled)
        {
            return;
        }

        Write("\x1b[");
        Write(parameters);
        Write("m");
    }

    private void WriteCsiSgrCodes(List<int> codes)
    {
        Write("\x1b[");
        for (var i = 0; i < codes.Count; i++)
        {
            if (i > 0)
            {
                WriteChar(';');
            }

            WriteInt(codes[i]);
        }
        WriteChar('m');
    }

    private static AnsiStyle ResolveState(AnsiStyle style, AnsiStyle fallback)
    {
        var fg = style.Foreground ?? fallback.Foreground ?? AnsiColor.Default;
        var bg = style.Background ?? fallback.Background ?? AnsiColor.Default;
        return new AnsiStyle { Foreground = fg, Background = bg, Decorations = style.Decorations };
    }

    private static void BuildSgrTransitionCodes(AnsiStyle from, AnsiStyle to, AnsiCapabilities capabilities, List<int> codes)
    {
        var fromDec = from.Decorations;
        var toDec = to.Decorations;

        var intensityChanged = ((fromDec ^ toDec) & (AnsiDecorations.Bold | AnsiDecorations.Dim)) != 0;
        if (intensityChanged)
        {
            codes.Add(22);
            if ((toDec & AnsiDecorations.Bold) != 0)
            {
                codes.Add(1);
            }
            if ((toDec & AnsiDecorations.Dim) != 0)
            {
                codes.Add(2);
            }
        }

        var toEnable = toDec & ~fromDec & ~(AnsiDecorations.Bold | AnsiDecorations.Dim);
        var toDisable = fromDec & ~toDec & ~(AnsiDecorations.Bold | AnsiDecorations.Dim);

        AddDecorationDisableCodes(toDisable, codes);
        AddDecorationEnableCodes(toEnable, codes);

        if (capabilities.ColorLevel != AnsiColorLevel.None)
        {
            if (!Equals(from.Foreground, to.Foreground) && to.Foreground is { } fg)
            {
                AddColorCodes(isForeground: true, fg, capabilities, codes);
            }

            if (!Equals(from.Background, to.Background) && to.Background is { } bg)
            {
                AddColorCodes(isForeground: false, bg, capabilities, codes);
            }
        }
    }

    private void WriteColorSgr(bool isForeground, AnsiColor color)
    {
        _codes.Clear();
        AddColorCodes(isForeground, color, Capabilities, _codes);
        if (_codes.Count > 0)
        {
            WriteCsiSgrCodes(_codes);
        }
    }

    private static void AddColorCodes(bool isForeground, AnsiColor color, AnsiCapabilities capabilities, List<int> codes)
    {
        // Color encoding rules (SGR):
        // - Default: 39 (foreground) / 49 (background)
        // - Basic16: 30–37, 90–97 (foreground) and 40–47, 100–107 (background)
        // - Indexed256: 38;5;n (foreground) / 48;5;n (background)
        // - TrueColor: 38;2;r;g;b / 48;2;r;g;b
        if (!color.TryDowngrade(capabilities.ColorLevel, out var downgraded))
        {
            return;
        }

        switch (downgraded.Kind)
        {
            case AnsiColorKind.Default:
                codes.Add(isForeground ? 39 : 49);
                return;
            case AnsiColorKind.Basic16:
                {
                    var index = downgraded.Index;
                    if (index < 8)
                    {
                        codes.Add((isForeground ? 30 : 40) + index);
                    }
                    else
                    {
                        codes.Add((isForeground ? 90 : 100) + (index - 8));
                    }
                    return;
                }
            case AnsiColorKind.Indexed256:
                codes.Add(isForeground ? 38 : 48);
                codes.Add(5);
                codes.Add(downgraded.Index);
                return;
            case AnsiColorKind.Rgb:
                if (capabilities.ColorLevel != AnsiColorLevel.TrueColor)
                {
                    AddColorCodes(isForeground, AnsiColorPalette.ToXterm256(downgraded.R, downgraded.G, downgraded.B), capabilities, codes);
                    return;
                }
                codes.Add(isForeground ? 38 : 48);
                codes.Add(2);
                codes.Add(downgraded.R);
                codes.Add(downgraded.G);
                codes.Add(downgraded.B);
                return;
            default:
                return;
        }
    }

    private static void AddDecorationEnableCodes(AnsiDecorations decorations, List<int> codes)
    {
        // SGR enable parameters:
        // 1 bold, 2 dim, 3 italic, 4 underline, 5 blink, 7 invert, 8 hidden, 9 strikethrough.
        if ((decorations & AnsiDecorations.Bold) != 0)
        {
            codes.Add(1);
        }
        if ((decorations & AnsiDecorations.Dim) != 0)
        {
            codes.Add(2);
        }
        if ((decorations & AnsiDecorations.Italic) != 0)
        {
            codes.Add(3);
        }
        if ((decorations & AnsiDecorations.Underline) != 0)
        {
            codes.Add(4);
        }
        if ((decorations & AnsiDecorations.Blink) != 0)
        {
            codes.Add(5);
        }
        if ((decorations & AnsiDecorations.Invert) != 0)
        {
            codes.Add(7);
        }
        if ((decorations & AnsiDecorations.Hidden) != 0)
        {
            codes.Add(8);
        }
        if ((decorations & AnsiDecorations.Strikethrough) != 0)
        {
            codes.Add(9);
        }
    }

    private static void AddDecorationDisableCodes(AnsiDecorations decorations, List<int> codes)
    {
        // SGR disable parameters:
        // 22 normal intensity (clears bold+dim), 23 not italic, 24 not underlined,
        // 25 steady (not blinking), 27 positive image (not inverted), 28 reveal (not hidden), 29 not crossed out.
        if ((decorations & (AnsiDecorations.Bold | AnsiDecorations.Dim)) != 0)
        {
            codes.Add(22);
        }
        if ((decorations & AnsiDecorations.Italic) != 0)
        {
            codes.Add(23);
        }
        if ((decorations & AnsiDecorations.Underline) != 0)
        {
            codes.Add(24);
        }
        if ((decorations & AnsiDecorations.Blink) != 0)
        {
            codes.Add(25);
        }
        if ((decorations & AnsiDecorations.Invert) != 0)
        {
            codes.Add(27);
        }
        if ((decorations & AnsiDecorations.Hidden) != 0)
        {
            codes.Add(28);
        }
        if ((decorations & AnsiDecorations.Strikethrough) != 0)
        {
            codes.Add(29);
        }
    }
}
