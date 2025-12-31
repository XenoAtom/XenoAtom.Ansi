// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi.Helpers;
using XenoAtom.Ansi.Tokens;

namespace XenoAtom.Ansi;

/// <summary>
/// Tokenizes ANSI/VT escape sequences from a stream of UTF-16 characters.
/// </summary>
/// <remarks>
/// This tokenizer is designed to be:
/// <list type="bullet">
/// <item><description><b>Streaming</b>: sequences may span multiple chunks; pass <c>isFinalChunk: false</c> to keep state</description></item>
/// <item><description><b>Tolerant</b>: malformed sequences never throw; they are surfaced as <see cref="UnknownEscapeToken"/></description></item>
/// <item><description><b>Mostly syntactic</b>: CSI/OSC are tokenized without deep semantics, except optional SGR decoding</description></item>
/// </list>
///
/// ANSI terminology:
/// <list type="bullet">
/// <item><description><b>CSI</b> (Control Sequence Introducer): typically <c>ESC [</c>, ends with a final byte in 0x40–0x7E</description></item>
/// <item><description><b>SGR</b> (Select Graphic Rendition): CSI with final byte <c>m</c></description></item>
/// <item><description><b>OSC</b> (Operating System Command): starts with <c>ESC ]</c>, terminated by BEL or ST (<c>ESC \\</c>)</description></item>
/// </list>
///
/// References (terminology and byte ranges):
/// <list type="bullet">
/// <item><description>ECMA-48 / ISO/IEC 6429: control functions and escape sequence structure</description></item>
/// <item><description>xterm control sequences (OSC 8 hyperlinks, common private modes)</description></item>
/// </list>
/// </remarks>
public sealed class AnsiTokenizer : IDisposable
{
    // The state machine is modeled after the well-known ECMA-48 / ISO/IEC 6429 parser model:
    // - Ground: normal text
    // - Escape: after ESC (0x1B)
    // - CSI: after ESC [
    // - OSC: after ESC ]
    // - DCS/APC/PM/SOS: string-like sequences terminated by ST (ESC \)
    private enum State
    {
        Ground = 0,
        Escape = 1,
        EscIntermediate = 2,
        Csi = 3,
        Osc = 4,
        OscMaybeSt = 5,
        DcsOrIgnored = 6,
        DcsOrIgnoredMaybeSt = 7,
    }

    private State _state;
    private readonly PooledCharBuffer _escapeBuffer = new(128);
    private readonly PooledCharBuffer _csiParamBuffer = new(64);
    private readonly PooledCharBuffer _csiIntermediateBuffer = new(16);
    private readonly PooledCharBuffer _escIntermediateBuffer = new(8);
    private char? _csiPrivateMarker;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnsiTokenizer"/> class.
    /// </summary>
    /// <param name="options">Tokenizer options and safety limits.</param>
    public AnsiTokenizer(AnsiTokenizerOptions options = default)
    {
        Options = options == default ? AnsiTokenizerOptions.Default : options;
    }

    /// <summary>
    /// Gets the options used by this tokenizer.
    /// </summary>
    public AnsiTokenizerOptions Options { get; }

    /// <summary>
    /// Resets the tokenizer to its initial state, discarding any buffered partial sequence.
    /// </summary>
    public void Reset()
    {
        _state = State.Ground;
        _escapeBuffer.Clear();
        _csiParamBuffer.Clear();
        _csiIntermediateBuffer.Clear();
        _escIntermediateBuffer.Clear();
        _csiPrivateMarker = null;
    }

    /// <summary>
    /// Tokenizes a chunk and returns a new list of tokens.
    /// </summary>
    /// <param name="chunk">The input chunk.</param>
    /// <param name="isFinalChunk">
    /// <see langword="true"/> to flush any unterminated sequence as <see cref="UnknownEscapeToken"/>;
    /// <see langword="false"/> to keep the internal state for the next chunk.
    /// </param>
    public List<AnsiToken> Tokenize(ReadOnlySpan<char> chunk, bool isFinalChunk = true)
    {
        var tokens = new List<AnsiToken>(4);
        Tokenize(chunk, isFinalChunk, tokens);
        return tokens;
    }

    /// <summary>
    /// Tokenizes a chunk and appends tokens to the specified list.
    /// </summary>
    /// <param name="chunk">The input chunk.</param>
    /// <param name="isFinalChunk">
    /// <see langword="true"/> to flush any unterminated sequence as <see cref="UnknownEscapeToken"/>;
    /// <see langword="false"/> to keep the internal state for the next chunk.
    /// </param>
    /// <param name="tokens">The destination token list.</param>
    public void Tokenize(ReadOnlySpan<char> chunk, bool isFinalChunk, List<AnsiToken> tokens)
    {
        if (tokens is null)
        {
            throw new ArgumentNullException(nameof(tokens));
        }

        if (Options.MaxTokenCountPerChunk <= 0)
        {
            throw new InvalidOperationException($"{nameof(AnsiTokenizerOptions.MaxTokenCountPerChunk)} must be > 0.");
        }

        var i = 0;
        var textStart = 0;

        static void FlushText(ReadOnlySpan<char> source, ref int textStart, int endExclusive, List<AnsiToken> tokens)
        {
            if (endExclusive > textStart)
            {
                tokens.Add(new TextToken(new string(source.Slice(textStart, endExclusive - textStart))));
            }
            textStart = endExclusive;
        }

        while (i < chunk.Length)
        {
            if (tokens.Count >= Options.MaxTokenCountPerChunk)
            {
                FlushText(chunk, ref textStart, chunk.Length, tokens);
                Reset();
                return;
            }

            var c = chunk[i];
            switch (_state)
            {
                case State.Ground:
                    if (c == '\x1b')
                    {
                        FlushText(chunk, ref textStart, i, tokens);
                        _escapeBuffer.Clear();
                        _escapeBuffer.Append(c);
                        _state = State.Escape;
                        i++;
                        textStart = i;
                        continue;
                    }

                    // C1 control codes (8-bit) used by some terminals/streams:
                    // - CSI: 0x9B
                    // - OSC: 0x9D
                    // - DCS: 0x90
                    // - PM:  0x9E
                    // - APC: 0x9F
                    if (c is '\x9b' or '\x9d' or '\x90' or '\x9e' or '\x9f')
                    {
                        FlushText(chunk, ref textStart, i, tokens);
                        _escapeBuffer.Clear();
                        _escapeBuffer.Append(c);

                        if (c == '\x9b')
                        {
                            _csiParamBuffer.Clear();
                            _csiIntermediateBuffer.Clear();
                            _csiPrivateMarker = null;
                            _state = State.Csi;
                        }
                        else if (c == '\x9d')
                        {
                            _state = State.Osc;
                        }
                        else
                        {
                            _state = State.DcsOrIgnored;
                        }

                        i++;
                        textStart = i;
                        continue;
                    }

                    if (IsTokenizedControl(c))
                    {
                        FlushText(chunk, ref textStart, i, tokens);
                        tokens.Add(new ControlToken(c));
                        i++;
                        textStart = i;
                        continue;
                    }

                    i++;
                    continue;

                case State.Escape:
                    if (c == '[')
                    {
                        _escapeBuffer.Append(c);
                        _csiParamBuffer.Clear();
                        _csiIntermediateBuffer.Clear();
                        _csiPrivateMarker = null;
                        _state = State.Csi;
                        i++;
                        textStart = i;
                        continue;
                    }

                    if (c == ']')
                    {
                        _escapeBuffer.Append(c);
                        _state = State.Osc;
                        i++;
                        textStart = i;
                        continue;
                    }

                    if (c is 'P' or 'X' or '^' or '_')
                    {
                        _escapeBuffer.Append(c);
                        _state = State.DcsOrIgnored;
                        i++;
                        textStart = i;
                        continue;
                    }

                    _escapeBuffer.Append(c);
                    if (IsEscIntermediateByte(c))
                    {
                        _escIntermediateBuffer.Clear();
                        _escIntermediateBuffer.Append(c);
                        _state = State.EscIntermediate;
                    }
                    else if (IsEscFinalByte(c))
                    {
                        tokens.Add(new EscToken(string.Empty, c, _escapeBuffer.ToStringAndClear()));
                        _state = State.Ground;
                    }
                    else
                    {
                        tokens.Add(new UnknownEscapeToken(_escapeBuffer.ToStringAndClear()));
                        _state = State.Ground;
                    }
                    i++;
                    textStart = i;
                    continue;

                case State.EscIntermediate:
                    _escapeBuffer.Append(c);
                    if (_escapeBuffer.Length > Options.MaxEscapeSequenceLength)
                    {
                        tokens.Add(new UnknownEscapeToken(_escapeBuffer.ToStringAndClear()));
                        Reset();
                        i++;
                        textStart = i;
                        continue;
                    }

                    if (IsEscIntermediateByte(c))
                    {
                        _escIntermediateBuffer.Append(c);
                        i++;
                        textStart = i;
                        continue;
                    }

                    if (IsEscFinalByte(c))
                    {
                        var intermediates = _escIntermediateBuffer.ToStringAndClear();
                        tokens.Add(new EscToken(intermediates, c, _escapeBuffer.ToStringAndClear()));
                        _state = State.Ground;
                        i++;
                        textStart = i;
                        continue;
                    }

                    tokens.Add(new UnknownEscapeToken(_escapeBuffer.ToStringAndClear()));
                    Reset();
                    i++;
                    textStart = i;
                    continue;

                case State.Csi:
                    // CSI grammar (ECMA-48):
                    //   CSI = ESC '['
                    //   parameter bytes 0x30–0x3F (digits + separators)
                    //   intermediate bytes 0x20–0x2F
                    //   final byte 0x40–0x7E
                    //
                    // We additionally capture a single "private marker" (<,=,>,?) if it appears first.
                    _escapeBuffer.Append(c);
                    if (_escapeBuffer.Length > Options.MaxEscapeSequenceLength)
                    {
                        tokens.Add(new UnknownEscapeToken(_escapeBuffer.ToStringAndClear()));
                        Reset();
                        i++;
                        textStart = i;
                        continue;
                    }

                    if (IsCsiParameterByte(c))
                    {
                        if (_csiParamBuffer.Length == 0 && _csiIntermediateBuffer.Length == 0 && _csiPrivateMarker is null && IsPrivateMarker(c))
                        {
                            _csiPrivateMarker = c;
                        }
                        else
                        {
                            _csiParamBuffer.Append(c);
                        }

                        i++;
                        textStart = i;
                        continue;
                    }

                    if (IsCsiIntermediateByte(c))
                    {
                        _csiIntermediateBuffer.Append(c);
                        i++;
                        textStart = i;
                        continue;
                    }

                    if (IsCsiFinalByte(c))
                    {
                        var raw = _escapeBuffer.ToStringAndClear();
                        var intermediates = _csiIntermediateBuffer.ToStringAndClear();
                        var parameters = ParseCsiParameters(_csiParamBuffer.AsSpan());
                        _csiParamBuffer.Clear();

                        if (Options.DecodeSgr && c == 'm' && intermediates.Length == 0 && _csiPrivateMarker is null)
                        {
                            tokens.Add(new SgrToken(DecodeSgr(parameters), raw));
                        }
                        else
                        {
                            tokens.Add(new CsiToken(intermediates, parameters, c, _csiPrivateMarker, raw));
                        }

                        _csiPrivateMarker = null;
                        _state = State.Ground;
                        i++;
                        textStart = i;
                        continue;
                    }

                    tokens.Add(new UnknownEscapeToken(_escapeBuffer.ToStringAndClear()));
                    Reset();
                    i++;
                    textStart = i;
                    continue;

                case State.Osc:
                    // OSC (Operating System Command) is introduced by ESC ] and terminated by either:
                    // - BEL (0x07), or
                    // - ST (String Terminator), which is ESC \.
                    //
                    // We buffer until a terminator is found; if the buffer grows too large, we emit UnknownEscapeToken.
                    _escapeBuffer.Append(c);
                    if (_escapeBuffer.Length > Options.MaxOscLength)
                    {
                        tokens.Add(new UnknownEscapeToken(_escapeBuffer.ToStringAndClear()));
                        Reset();
                        i++;
                        textStart = i;
                        continue;
                    }

                    if (c == '\x07')
                    {
                        EmitOscToken(tokens, _escapeBuffer.ToStringAndClear());
                        _state = State.Ground;
                        i++;
                        textStart = i;
                        continue;
                    }

                    if (c == '\x9c')
                    {
                        EmitOscToken(tokens, _escapeBuffer.ToStringAndClear());
                        _state = State.Ground;
                        i++;
                        textStart = i;
                        continue;
                    }

                    if (c == '\x1b')
                    {
                        _state = State.OscMaybeSt;
                        i++;
                        textStart = i;
                        continue;
                    }

                    i++;
                    textStart = i;
                    continue;

                case State.OscMaybeSt:
                    // We saw ESC inside OSC; if the next character is '\', it's ST (ESC \) and terminates the OSC.
                    _escapeBuffer.Append(c);
                    if (c == '\\')
                    {
                        EmitOscToken(tokens, _escapeBuffer.ToStringAndClear());
                        _state = State.Ground;
                        i++;
                        textStart = i;
                        continue;
                    }

                    if (c == '\x9c')
                    {
                        EmitOscToken(tokens, _escapeBuffer.ToStringAndClear());
                        _state = State.Ground;
                        i++;
                        textStart = i;
                        continue;
                    }

                    _state = State.Osc;
                    i++;
                    textStart = i;
                    continue;

                case State.DcsOrIgnored:
                    // DCS/APC/PM/SOS are "string" functions in ECMA-48 which are terminated by ST (ESC \).
                    // This library does not decode these; we skip until ST and surface the buffered sequence as UnknownEscapeToken.
                    _escapeBuffer.Append(c);
                    if (_escapeBuffer.Length > Options.MaxEscapeSequenceLength)
                    {
                        tokens.Add(new UnknownEscapeToken(_escapeBuffer.ToStringAndClear()));
                        Reset();
                        i++;
                        textStart = i;
                        continue;
                    }

                    if (c == '\x1b')
                    {
                        _state = State.DcsOrIgnoredMaybeSt;
                        i++;
                        textStart = i;
                        continue;
                    }

                    if (c == '\x9c')
                    {
                        tokens.Add(new UnknownEscapeToken(_escapeBuffer.ToStringAndClear()));
                        _state = State.Ground;
                        i++;
                        textStart = i;
                        continue;
                    }

                    i++;
                    textStart = i;
                    continue;

                case State.DcsOrIgnoredMaybeSt:
                    // We saw ESC inside DCS/APC/PM/SOS; '\'? indicates ST (ESC \) terminator.
                    _escapeBuffer.Append(c);
                    if (c == '\\')
                    {
                        tokens.Add(new UnknownEscapeToken(_escapeBuffer.ToStringAndClear()));
                        _state = State.Ground;
                        i++;
                        textStart = i;
                        continue;
                    }

                    _state = State.DcsOrIgnored;
                    i++;
                    textStart = i;
                    continue;

                default:
                    throw new InvalidOperationException($"Unknown state: {_state}.");
            }
        }

        if (_state == State.Ground)
        {
            if (chunk.Length > textStart)
            {
                tokens.Add(new TextToken(new string(chunk.Slice(textStart))));
            }
        }
        else
        {
            // When not final, keep the buffered sequence. When final, flush it as unknown.
            if (chunk.Length > textStart)
            {
                _escapeBuffer.Append(chunk.Slice(textStart));
            }

            if (isFinalChunk)
            {
                tokens.Add(new UnknownEscapeToken(_escapeBuffer.ToStringAndClear()));
                Reset();
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _escapeBuffer.Dispose();
        _csiParamBuffer.Dispose();
        _csiIntermediateBuffer.Dispose();
        _escIntermediateBuffer.Dispose();
    }

    // We surface a small subset of C0 controls that are commonly meaningful to a renderer.
    // Other control characters are left in text.
    private static bool IsTokenizedControl(char c) => c is '\r' or '\n' or '\t' or '\x07';

    // CSI private markers are in the parameter byte range (0x3C–0x3F) and are used heavily by DEC/xterm.
    // Example: "ESC [ ? 25 l" (DEC private mode, hide cursor).
    private static bool IsPrivateMarker(char c) => c is '<' or '=' or '>' or '?';

    // CSI parameter bytes are 0x30–0x3F (digits and separators), per ECMA-48.
    private static bool IsCsiParameterByte(char c) => c is >= (char)0x30 and <= (char)0x3F;

    // CSI intermediate bytes are 0x20–0x2F (rare in modern usage; used for "!" in soft reset: "ESC [ ! p").
    private static bool IsCsiIntermediateByte(char c) => c is >= (char)0x20 and <= (char)0x2F;

    // CSI final byte is 0x40–0x7E.
    private static bool IsCsiFinalByte(char c) => c is >= (char)0x40 and <= (char)0x7E;

    // General ESC sequences share intermediate bytes with CSI (0x20–0x2F), final byte 0x30–0x7E.
    // This covers sequences like "ESC 7" and "ESC 8" (DECSC/DECRC) and "ESC \" (ST).
    private static bool IsEscIntermediateByte(char c) => c is >= (char)0x20 and <= (char)0x2F;

    private static bool IsEscFinalByte(char c) => c is >= (char)0x30 and <= (char)0x7E;

    private static int[] ParseCsiParameters(ReadOnlySpan<char> rawParameters)
    {
        if (rawParameters.IsEmpty)
        {
            return [];
        }

        var list = new List<int>(4);
        var value = 0;
        var hasDigits = false;
        var sawAny = false;

        for (var i = 0; i < rawParameters.Length; i++)
        {
            var c = rawParameters[i];
            if (c is >= '0' and <= '9')
            {
                // Avoid throwing on overflow for untrusted input.
                var digit = c - '0';
                if (value > (int.MaxValue - digit) / 10)
                {
                    value = int.MaxValue;
                }
                else
                {
                    value = (value * 10) + digit;
                }
                hasDigits = true;
                sawAny = true;
            }
            else if (c is ';' or ':')
            {
                list.Add(hasDigits ? value : 0);
                value = 0;
                hasDigits = false;
                sawAny = true;
            }
            else
            {
                // tolerate
                sawAny = true;
            }
        }

        if (sawAny)
        {
            list.Add(hasDigits ? value : 0);
        }

        return list.ToArray();
    }

    private static void EmitOscToken(List<AnsiToken> tokens, string raw)
    {
        // raw: ESC ] ... BEL  OR  ESC ] ... ESC \  OR  OSC (0x9D) ... ST (0x9C)
        // We parse inside the brackets; tolerate malformed data.
        ReadOnlySpan<char> payload;
        if (raw.Length > 0 && raw[0] == '\x9d')
        {
            payload = raw.AsSpan(1); // after OSC (C1)
        }
        else
        {
            payload = raw.AsSpan(2); // after ESC ]
        }
        if (payload.Length == 0)
        {
            tokens.Add(new OscToken(-1, string.Empty, raw));
            return;
        }

        // trim terminator
        if (payload[^1] == '\x07')
        {
            payload = payload[..^1];
        }
        else if (payload[^1] == '\x9c')
        {
            payload = payload[..^1];
        }
        else if (payload.Length >= 2 && payload[^2] == '\x1b' && payload[^1] == '\\')
        {
            payload = payload[..^2];
        }

        var semiIndex = payload.IndexOf(';');
        if (semiIndex < 0)
        {
            tokens.Add(new OscToken(TryParseInt(payload, out var code) ? code : -1, string.Empty, raw));
            return;
        }

        var codeSpan = payload[..semiIndex];
        var dataSpan = payload[(semiIndex + 1)..];
        tokens.Add(new OscToken(TryParseInt(codeSpan, out var codeValue) ? codeValue : -1, dataSpan.ToString(), raw));
    }

    private static bool TryParseInt(ReadOnlySpan<char> span, out int value)
    {
        value = 0;
        if (span.IsEmpty)
        {
            return false;
        }

        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];
            if (c is < '0' or > '9')
            {
                return false;
            }

            var digit = c - '0';
            if (value > (int.MaxValue - digit) / 10)
            {
                value = int.MaxValue;
            }
            else
            {
                value = (value * 10) + digit;
            }
        }

        return true;
    }

    private static AnsiSgrOp[] DecodeSgr(int[] parameters)
    {
        // SGR (Select Graphic Rendition) is CSI ... m.
        // Common parameters:
        //   0 reset, 1 bold, 2 dim, 3 italic, 4 underline, 7 invert, 9 strike
        //   22/23/24/27/29 disable variants
        //   30–37/90–97 basic foreground, 40–47/100–107 basic background
        //   38;5;n / 48;5;n indexed 256-color
        //   38;2;r;g;b / 48;2;r;g;b truecolor
        if (parameters.Length == 0)
        {
            parameters = [0];
        }

        var ops = new List<AnsiSgrOp>(8);

        for (var i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            switch (p)
            {
                case 0:
                    ops.Add(AnsiSgrOp.Reset());
                    break;
                case 1:
                    ops.Add(AnsiSgrOp.SetDecoration(AnsiDecorations.Bold, enabled: true));
                    break;
                case 2:
                    ops.Add(AnsiSgrOp.SetDecoration(AnsiDecorations.Dim, enabled: true));
                    break;
                case 3:
                    ops.Add(AnsiSgrOp.SetDecoration(AnsiDecorations.Italic, enabled: true));
                    break;
                case 4:
                    ops.Add(AnsiSgrOp.SetDecoration(AnsiDecorations.Underline, enabled: true));
                    break;
                case 5:
                    ops.Add(AnsiSgrOp.SetDecoration(AnsiDecorations.Blink, enabled: true));
                    break;
                case 7:
                    ops.Add(AnsiSgrOp.SetDecoration(AnsiDecorations.Invert, enabled: true));
                    break;
                case 8:
                    ops.Add(AnsiSgrOp.SetDecoration(AnsiDecorations.Hidden, enabled: true));
                    break;
                case 9:
                    ops.Add(AnsiSgrOp.SetDecoration(AnsiDecorations.Strikethrough, enabled: true));
                    break;
                case 22:
                    ops.Add(AnsiSgrOp.SetDecoration(AnsiDecorations.Bold, enabled: false));
                    ops.Add(AnsiSgrOp.SetDecoration(AnsiDecorations.Dim, enabled: false));
                    break;
                case 23:
                    ops.Add(AnsiSgrOp.SetDecoration(AnsiDecorations.Italic, enabled: false));
                    break;
                case 24:
                    ops.Add(AnsiSgrOp.SetDecoration(AnsiDecorations.Underline, enabled: false));
                    break;
                case 25:
                    ops.Add(AnsiSgrOp.SetDecoration(AnsiDecorations.Blink, enabled: false));
                    break;
                case 27:
                    ops.Add(AnsiSgrOp.SetDecoration(AnsiDecorations.Invert, enabled: false));
                    break;
                case 28:
                    ops.Add(AnsiSgrOp.SetDecoration(AnsiDecorations.Hidden, enabled: false));
                    break;
                case 29:
                    ops.Add(AnsiSgrOp.SetDecoration(AnsiDecorations.Strikethrough, enabled: false));
                    break;
                case 39:
                    ops.Add(AnsiSgrOp.SetForeground(AnsiColor.Default));
                    break;
                case 49:
                    ops.Add(AnsiSgrOp.SetBackground(AnsiColor.Default));
                    break;
                default:
                    if (p is >= 30 and <= 37)
                    {
                        ops.Add(AnsiSgrOp.SetForeground(AnsiColor.Basic16(p - 30)));
                    }
                    else if (p is >= 90 and <= 97)
                    {
                        ops.Add(AnsiSgrOp.SetForeground(AnsiColor.Basic16(8 + (p - 90))));
                    }
                    else if (p is >= 40 and <= 47)
                    {
                        ops.Add(AnsiSgrOp.SetBackground(AnsiColor.Basic16(p - 40)));
                    }
                    else if (p is >= 100 and <= 107)
                    {
                        ops.Add(AnsiSgrOp.SetBackground(AnsiColor.Basic16(8 + (p - 100))));
                    }
                    else if (p is 38 or 48)
                    {
                        var isForeground = p == 38;
                        if (i + 1 >= parameters.Length)
                        {
                            break;
                        }

                        var mode = parameters[++i];
                        if (mode == 5)
                        {
                            if (i + 1 < parameters.Length)
                            {
                                var index = parameters[++i];
                                if (index is >= 0 and <= 255)
                                {
                                    ops.Add(isForeground
                                        ? AnsiSgrOp.SetForeground(AnsiColor.Indexed256(index))
                                        : AnsiSgrOp.SetBackground(AnsiColor.Indexed256(index)));
                                }
                            }
                        }
                        else if (mode == 2)
                        {
                            if (i + 3 < parameters.Length)
                            {
                                var r = parameters[++i];
                                var g = parameters[++i];
                                var b = parameters[++i];
                                if (r is >= 0 and <= 255 && g is >= 0 and <= 255 && b is >= 0 and <= 255)
                                {
                                    ops.Add(isForeground
                                        ? AnsiSgrOp.SetForeground(AnsiColor.Rgb((byte)r, (byte)g, (byte)b))
                                        : AnsiSgrOp.SetBackground(AnsiColor.Rgb((byte)r, (byte)g, (byte)b)));
                                }
                            }
                        }
                    }
                    break;
            }
        }

        return ops.ToArray();
    }
}
