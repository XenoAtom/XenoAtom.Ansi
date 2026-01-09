// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using System.Runtime.CompilerServices;

namespace XenoAtom.Ansi;

/// <summary>
/// Formats strings using a lightweight markup syntax and emits ANSI/VT sequences.
/// </summary>
/// <remarks>
/// <para>
/// Supported syntax:
/// </para>
/// <list type="bullet">
/// <item><description><c>[red]</c>, <c>[bold]</c>, <c>[underline]</c>, <c>[bold yellow on blue]</c></description></item>
/// <item><description><c>[/]</c> closes the most recent tag (nesting is supported).</description></item>
/// <item><description><c>[[</c> escapes a literal <c>[</c>, and <c>]]</c> escapes a literal <c>]</c>.</description></item>
/// <item><description>Colors: basic-16 names and <c>bright*</c> variants (e.g. <c>brightred</c>, <c>bright-red</c>), plus <c>#RRGGBB</c>.</description></item>
/// <item><description>Background: <c>on &lt;color&gt;</c>, <c>bg:&lt;color&gt;</c>, or <c>bg=&lt;color&gt;</c>.</description></item>
/// </list>
/// </remarks>
public sealed class AnsiMarkup
{
    private readonly IAnsiBasicWriter _writer;
    private readonly List<AnsiStyle> _styleStack;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnsiMarkup"/> class that writes to an existing <see cref="AnsiWriter"/>.
    /// </summary>
    /// <param name="writer">The target writer to append markup output to.</param>
    public AnsiMarkup(AnsiWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _styleStack = new List<AnsiStyle>(8);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnsiMarkup"/> class that writes to an existing <see cref="IAnsiBasicWriter"/>.
    /// </summary>
    /// <param name="writer">The target writer to append markup output to.</param>
    public AnsiMarkup(IAnsiBasicWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _styleStack = new List<AnsiStyle>(8);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnsiMarkup"/> class that writes to an <see cref="IBufferWriter{T}"/>.
    /// </summary>
    public AnsiMarkup(IBufferWriter<char> bufferWriter, AnsiCapabilities? capabilities = null)
        : this(new AnsiWriter(bufferWriter ?? throw new ArgumentNullException(nameof(bufferWriter)), capabilities))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnsiMarkup"/> class that writes to a <see cref="TextWriter"/>.
    /// </summary>
    public AnsiMarkup(TextWriter textWriter, AnsiCapabilities? capabilities = null)
        : this(new AnsiWriter(textWriter ?? throw new ArgumentNullException(nameof(textWriter)), capabilities))
    {
    }

    /// <summary>
    /// Gets the capabilities used by this writer.
    /// </summary>
    public AnsiCapabilities Capabilities => _writer.Capabilities;

    /// <summary>
    /// Writes formatted markup to the underlying writer.
    /// </summary>
    /// <returns>This instance, for fluent chaining.</returns>
    public AnsiMarkup Write(ReadOnlySpan<char> markup)
    {
        _styleStack.Clear();
        var current = AnsiStyle.Default;
        AppendTo(_writer, markup, _styleStack, ref current);
        return this;
    }

    /// <summary>
    /// Writes formatted markup to the underlying writer.
    /// </summary>
    /// <returns>This instance, for fluent chaining.</returns>
    public AnsiMarkup Write(string markup)
    {
        if (string.IsNullOrEmpty(markup))
        {
            return this;
        }

        return Write(markup.AsSpan());
    }

    /// <summary>
    /// Writes the specified text verbatim, without interpreting markup.
    /// </summary>
    /// <remarks>
    /// This is useful for writing user input safely (no markup injection), or for emitting literal markup text.
    /// </remarks>
    /// <returns>This instance, for fluent chaining.</returns>
    public AnsiMarkup WriteEscape(ReadOnlySpan<char> text)
    {
        if (!text.IsEmpty)
        {
            _writer.Write(text);
        }

        return this;
    }

    /// <summary>
    /// Writes the specified text verbatim, without interpreting markup.
    /// </summary>
    /// <remarks>
    /// This is useful for writing user input safely (no markup injection), or for emitting literal markup text.
    /// </remarks>
    /// <returns>This instance, for fluent chaining.</returns>
    public AnsiMarkup WriteEscape(string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            _writer.Write(text);
        }

        return this;
    }

    /// <summary>
    /// Writes formatted markup provided via an interpolated string to the underlying writer.
    /// </summary>
    /// <remarks>
    /// Interpolated values are escaped so that user input cannot inject markup tags.
    /// </remarks>
    /// <returns>This instance, for fluent chaining.</returns>
    public AnsiMarkup Write(ref AnsiMarkupInterpolatedStringHandler markup)
    {
        try
        {
            return Write(markup.WrittenSpan);
        }
        finally
        {
            markup.Dispose();
        }
    }

    /// <summary>
    /// Renders markup into a string.
    /// </summary>
    public static string Render(ReadOnlySpan<char> markup, AnsiCapabilities? capabilities = null, int initialCapacity = 256)
    {
        using var builder = new AnsiBuilder(initialCapacity);
        var writer = new AnsiWriter(builder, capabilities);
        var formatter = new AnsiMarkup(writer);
        formatter.Write(markup);
        return builder.ToString();
    }

    /// <summary>
    /// Renders markup provided via an interpolated string and returns the resulting ANSI string.
    /// </summary>
    /// <remarks>
    /// Interpolated values are escaped so that user input cannot inject markup tags.
    /// </remarks>
    public static string Render(ref AnsiMarkupInterpolatedStringHandler markup, AnsiCapabilities? capabilities = null, int initialCapacity = 256)
    {
        try
        {
            return Render(markup.WrittenSpan, capabilities, initialCapacity);
        }
        finally
        {
            markup.Dispose();
        }
    }

    /// <summary>
    /// Escapes a string so it can be safely embedded in markup (i.e. <c>[</c> becomes <c>[[</c> and <c>]</c> becomes <c>]]</c>).
    /// </summary>
    public static string Escape(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return string.Empty;
        }

        var extra = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '[' || c == ']')
            {
                extra++;
            }
        }

        if (extra == 0)
        {
            return text.ToString();
        }

        using var builder = new AnsiBuilder(text.Length + extra);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '[')
            {
                builder.Append("[[");
            }
            else if (c == ']')
            {
                builder.Append("]]");
            }
            else
            {
                var span = builder.GetSpan(1);
                span[0] = c;
                builder.Advance(1);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Appends rendered markup to an existing <see cref="AnsiWriter"/>.
    /// </summary>
    public static void AppendTo(AnsiWriter writer, ReadOnlySpan<char> markup)
    {
        ArgumentNullException.ThrowIfNull(writer);

        var styleStack = new List<AnsiStyle>(8);
        var current = AnsiStyle.Default;
        AppendTo(writer, markup, styleStack, ref current);
    }

    /// <summary>
    /// Appends rendered markup to an existing <see cref="IAnsiBasicWriter"/>.
    /// </summary>
    public static void AppendTo(IAnsiBasicWriter writer, ReadOnlySpan<char> markup)
    {
        ArgumentNullException.ThrowIfNull(writer);

        var styleStack = new List<AnsiStyle>(8);
        var current = AnsiStyle.Default;
        AppendTo(writer, markup, styleStack, ref current);
    }

    private static void AppendTo(IAnsiBasicWriter writer, ReadOnlySpan<char> markup, List<AnsiStyle> styleStack, ref AnsiStyle currentStyle)
    {
        if (markup.IsEmpty)
        {
            return;
        }

        var lastTextStart = 0;

        for (var i = 0; i < markup.Length; i++)
        {
            var c = markup[i];

            if (c == '[')
            {
                if (i + 1 < markup.Length && markup[i + 1] == '[')
                {
                    if (i > lastTextStart)
                    {
                        writer.Write(markup.Slice(lastTextStart, i - lastTextStart));
                    }
                    writer.Write("[");
                    i++;
                    lastTextStart = i + 1;
                    continue;
                }

                var close = markup.Slice(i + 1).IndexOf(']');
                if (close < 0)
                {
                    continue;
                }

                if (i > lastTextStart)
                {
                    writer.Write(markup.Slice(lastTextStart, i - lastTextStart));
                }

                var tagStart = i + 1;
                var tagLen = close;
                var tag = markup.Slice(tagStart, tagLen);

                if (!TryProcessTag(writer, tag, styleStack, ref currentStyle))
                {
                    writer.Write(markup.Slice(i, tagLen + 2));
                }

                i = i + tagLen + 1;
                lastTextStart = i + 1;
                continue;
            }

            if (c == ']' && i + 1 < markup.Length && markup[i + 1] == ']')
            {
                if (i > lastTextStart)
                {
                    writer.Write(markup.Slice(lastTextStart, i - lastTextStart));
                }
                writer.Write("]");
                i++;
                lastTextStart = i + 1;
            }
        }

        if (lastTextStart < markup.Length)
        {
            writer.Write(markup.Slice(lastTextStart));
        }

        if (styleStack.Count > 0)
        {
            writer.StyleTransition(currentStyle, AnsiStyle.Default);
            currentStyle = AnsiStyle.Default;
            styleStack.Clear();
        }
    }

    private static bool TryProcessTag(IAnsiBasicWriter writer, ReadOnlySpan<char> tag, List<AnsiStyle> styleStack, ref AnsiStyle currentStyle)
    {
        tag = Trim(tag);
        if (tag.IsEmpty)
        {
            return false;
        }

        if (tag[0] == '/')
        {
            var previous = styleStack.Count > 0 ? styleStack[^1] : AnsiStyle.Default;
            if (styleStack.Count > 0)
            {
                styleStack.RemoveAt(styleStack.Count - 1);
            }

            writer.StyleTransition(currentStyle, previous);
            currentStyle = previous;
            return true;
        }

        var nextStyle = currentStyle;
        var recognized = false;

        var index = 0;
        while (TryReadToken(tag, ref index, out var token))
        {
            if (token.IsEmpty)
            {
                continue;
            }

            // Trie-like dispatch based on the first character. This avoids repeatedly probing the token in several helpers.
            switch (ToLowerAsciiInvariant(token[0]))
            {
                case 'r':
                    if (AsciiEqualsIgnoreCase(token, "reset"))
                    {
                        nextStyle = AnsiStyle.Default;
                        recognized = true;
                        continue;
                    }
                    break;
                case 'd':
                    if (AsciiEqualsIgnoreCase(token, "default"))
                    {
                        nextStyle = AnsiStyle.Default;
                        recognized = true;
                        continue;
                    }
                    if (AsciiEqualsIgnoreCase(token, "dim"))
                    {
                        nextStyle = nextStyle with { Decorations = nextStyle.Decorations | AnsiDecorations.Dim };
                        recognized = true;
                        continue;
                    }
                    break;
                case 'b':
                    // bg:... / bg=...
                    if (token.Length >= 3 &&
                        ToLowerAsciiInvariant(token[1]) == 'g' &&
                        (token[2] == ':' || token[2] == '='))
                    {
                        if (!TryParseColorToken(token[3..], out var bgPrefixed))
                        {
                            return false;
                        }

                        nextStyle = nextStyle with { Background = bgPrefixed };
                        recognized = true;
                        continue;
                    }

                    if (AsciiEqualsIgnoreCase(token, "bold"))
                    {
                        nextStyle = nextStyle with { Decorations = nextStyle.Decorations | AnsiDecorations.Bold };
                        recognized = true;
                        continue;
                    }
                    if (AsciiEqualsIgnoreCase(token, "blink"))
                    {
                        nextStyle = nextStyle with { Decorations = nextStyle.Decorations | AnsiDecorations.Blink };
                        recognized = true;
                        continue;
                    }
                    break;
                case 'f':
                    // fg:... / fg=...
                    if (token.Length >= 3 &&
                        ToLowerAsciiInvariant(token[1]) == 'g' &&
                        (token[2] == ':' || token[2] == '='))
                    {
                        if (!TryParseColorToken(token[3..], out var fgPrefixed))
                        {
                            return false;
                        }

                        nextStyle = nextStyle with { Foreground = fgPrefixed };
                        recognized = true;
                        continue;
                    }
                    break;
                case 'o':
                    if (token.Length == 2 && token[1] is 'n' or 'N')
                    {
                        if (!TryReadToken(tag, ref index, out var colorToken) || !TryParseColorToken(colorToken, out var bg))
                        {
                            return false;
                        }

                        nextStyle = nextStyle with { Background = bg };
                        recognized = true;
                        continue;
                    }
                    break;
                case 'i':
                    if (AsciiEqualsIgnoreCase(token, "italic"))
                    {
                        nextStyle = nextStyle with { Decorations = nextStyle.Decorations | AnsiDecorations.Italic };
                        recognized = true;
                        continue;
                    }
                    if (AsciiEqualsIgnoreCase(token, "invert") || AsciiEqualsIgnoreCase(token, "inverse"))
                    {
                        nextStyle = nextStyle with { Decorations = nextStyle.Decorations | AnsiDecorations.Invert };
                        recognized = true;
                        continue;
                    }
                    break;
                case 'u':
                    if (AsciiEqualsIgnoreCase(token, "underline"))
                    {
                        nextStyle = nextStyle with { Decorations = nextStyle.Decorations | AnsiDecorations.Underline };
                        recognized = true;
                        continue;
                    }
                    break;
                case 'h':
                    if (AsciiEqualsIgnoreCase(token, "hidden"))
                    {
                        nextStyle = nextStyle with { Decorations = nextStyle.Decorations | AnsiDecorations.Hidden };
                        recognized = true;
                        continue;
                    }
                    break;
                case 's':
                    if (AsciiEqualsIgnoreCase(token, "strikethrough") || AsciiEqualsIgnoreCase(token, "strike"))
                    {
                        nextStyle = nextStyle with { Decorations = nextStyle.Decorations | AnsiDecorations.Strikethrough };
                        recognized = true;
                        continue;
                    }
                    break;
            }

            if (TryParseColorToken(token, out var fg))
            {
                nextStyle = nextStyle with { Foreground = fg };
                recognized = true;
            }
        }

        if (!recognized)
        {
            return false;
        }

        styleStack.Add(currentStyle);
        writer.StyleTransition(currentStyle, nextStyle);
        currentStyle = nextStyle;
        return true;
    }

    private static bool TryParseColorToken(ReadOnlySpan<char> token, out AnsiColor color)
    {
        if (token.IsEmpty)
        {
            color = default;
            return false;
        }

        // Fast-path based on first character:
        // - #RRGGBB
        // - 0..255 (indexed)
        // - rgb(r,g,b)
        // - named colors (including bright/light prefixes)
        var first = token[0];
        if (first == '#')
        {
            return TryParseHexRgb(token, out color);
        }

        if ((uint)(first - '0') <= 9)
        {
            return TryParseIndexed256(token, out color);
        }

        if (ToLowerAsciiInvariant(first) == 'r' && AsciiStartsWithIgnoreCase(token, "rgb("))
        {
            return TryParseRgbFunction(token, out color);
        }

        return TryParseNamedColor(token, out color);
    }

    private static bool TryParseRgbFunction(ReadOnlySpan<char> token, out AnsiColor color)
    {
        // rgb(r,g,b)
        if (!AsciiStartsWithIgnoreCase(token, "rgb(") || token[^1] != ')')
        {
            color = default;
            return false;
        }

        var args = token.Slice(4, token.Length - 5);
        var i = 0;
        if (!TryReadNumber(args, ref i, out var r) ||
            !TryReadComma(args, ref i) ||
            !TryReadNumber(args, ref i, out var g) ||
            !TryReadComma(args, ref i) ||
            !TryReadNumber(args, ref i, out var b))
        {
            color = default;
            return false;
        }

        SkipWhitespace(args, ref i);
        if (i != args.Length)
        {
            color = default;
            return false;
        }

        if (r > 255 || g > 255 || b > 255)
        {
            color = default;
            return false;
        }

        color = AnsiColor.Rgb((byte)r, (byte)g, (byte)b);
        return true;
    }

    private static bool TryParseIndexed256(ReadOnlySpan<char> token, out AnsiColor color)
    {
        // 0..255
        var value = 0;
        for (var i = 0; i < token.Length; i++)
        {
            var c = token[i];
            if (c is < '0' or > '9')
            {
                color = default;
                return false;
            }

            value = (value * 10) + (c - '0');
            if (value > 255)
            {
                color = default;
                return false;
            }
        }

        color = AnsiColor.Indexed256(value);
        return true;
    }

    private static bool TryParseHexRgb(ReadOnlySpan<char> token, out AnsiColor color)
    {
        // #RRGGBB
        if (token.Length == 7 && token[0] == '#')
        {
            if (TryParseHexByte(token.Slice(1, 2), out var r) &&
                TryParseHexByte(token.Slice(3, 2), out var g) &&
                TryParseHexByte(token.Slice(5, 2), out var b))
            {
                color = AnsiColor.Rgb(r, g, b);
                return true;
            }
        }

        color = default;
        return false;
    }

    private static bool TryParseHexByte(ReadOnlySpan<char> token, out byte value)
    {
        if (token.Length != 2)
        {
            value = 0;
            return false;
        }

        static int HexValue(char c)
        {
            if (c is >= '0' and <= '9') return c - '0';
            if (c is >= 'a' and <= 'f') return (c - 'a') + 10;
            if (c is >= 'A' and <= 'F') return (c - 'A') + 10;
            return -1;
        }

        var hi = HexValue(token[0]);
        var lo = HexValue(token[1]);
        if (hi < 0 || lo < 0)
        {
            value = 0;
            return false;
        }

        value = (byte)((hi << 4) | lo);
        return true;
    }

    private static void SkipWhitespace(ReadOnlySpan<char> text, ref int index)
    {
        while (index < text.Length && IsWhitespace(text[index]))
        {
            index++;
        }
    }

    private static bool TryReadComma(ReadOnlySpan<char> text, ref int index)
    {
        SkipWhitespace(text, ref index);
        if (index >= text.Length || text[index] != ',')
        {
            return false;
        }

        index++;
        return true;
    }

    private static bool TryReadNumber(ReadOnlySpan<char> text, ref int index, out int value)
    {
        SkipWhitespace(text, ref index);
        if (index >= text.Length)
        {
            value = 0;
            return false;
        }

        var start = index;
        value = 0;

        while (index < text.Length)
        {
            var c = text[index];
            if (c is < '0' or > '9')
            {
                break;
            }

            value = (value * 10) + (c - '0');
            index++;
        }

        return index > start;
    }

    private static bool TryParseNamedColor(ReadOnlySpan<char> token, out AnsiColor color)
    {
        var isBright = false;
        if (token.IsEmpty)
        {
            color = default;
            return false;
        }

        // Trie-like dispatch based on first character.
        switch (ToLowerAsciiInvariant(token[0]))
        {
            case 'd':
                if (AsciiEqualsIgnoreCase(token, "default"))
                {
                    color = AnsiColor.Default;
                    return true;
                }
                break;
            case 'g':
                if (AsciiEqualsIgnoreCase(token, "gray") || AsciiEqualsIgnoreCase(token, "grey"))
                {
                    color = AnsiColors.BrightBlack;
                    return true;
                }
                break;
        }

        // Support "bright-*" / "light-*" prefixes.
        switch (ToLowerAsciiInvariant(token[0]))
        {
            case 'b':
                if (AsciiStartsWithIgnoreCase(token, "bright"))
                {
                    isBright = true;
                    token = TrimSeparators(token["bright".Length..]);
                }
                break;
            case 'l':
                if (AsciiStartsWithIgnoreCase(token, "light"))
                {
                    isBright = true;
                    token = TrimSeparators(token["light".Length..]);
                }
                break;
        }

        if (token.IsEmpty)
        {
            color = default;
            return false;
        }

        if (!TryGetBasicColorIndex(token, out var baseIndex))
        {
            color = default;
            return false;
        }

        color = AnsiColor.Basic16(isBright ? baseIndex + 8 : baseIndex);
        return true;
    }

    private static bool TryGetBasicColorIndex(ReadOnlySpan<char> token, out int baseIndex)
    {
        baseIndex = -1;
        if (token.IsEmpty)
        {
            return false;
        }

        switch (ToLowerAsciiInvariant(token[0]))
        {
            case 'b':
                if (AsciiEqualsIgnoreCase(token, "black"))
                {
                    baseIndex = 0;
                    return true;
                }
                if (AsciiEqualsIgnoreCase(token, "blue"))
                {
                    baseIndex = 4;
                    return true;
                }
                return false;
            case 'r':
                if (AsciiEqualsIgnoreCase(token, "red"))
                {
                    baseIndex = 1;
                    return true;
                }
                return false;
            case 'g':
                if (AsciiEqualsIgnoreCase(token, "green"))
                {
                    baseIndex = 2;
                    return true;
                }
                return false;
            case 'y':
                if (AsciiEqualsIgnoreCase(token, "yellow"))
                {
                    baseIndex = 3;
                    return true;
                }
                return false;
            case 'm':
                if (AsciiEqualsIgnoreCase(token, "magenta"))
                {
                    baseIndex = 5;
                    return true;
                }
                return false;
            case 'p':
                if (AsciiEqualsIgnoreCase(token, "purple"))
                {
                    baseIndex = 5;
                    return true;
                }
                return false;
            case 'c':
                if (AsciiEqualsIgnoreCase(token, "cyan"))
                {
                    baseIndex = 6;
                    return true;
                }
                return false;
            case 'a':
                if (AsciiEqualsIgnoreCase(token, "aqua"))
                {
                    baseIndex = 6;
                    return true;
                }
                return false;
            case 'w':
                if (AsciiEqualsIgnoreCase(token, "white"))
                {
                    baseIndex = 7;
                    return true;
                }
                return false;
            default:
                return false;
        }
    }

    private static bool TryReadToken(ReadOnlySpan<char> text, ref int index, out ReadOnlySpan<char> token)
    {
        while (index < text.Length && IsWhitespace(text[index]))
        {
            index++;
        }

        if (index >= text.Length)
        {
            token = default;
            return false;
        }

        var start = index;

        // Special-case rgb(...) tokens so they can contain spaces inside the parentheses.
        // Also supports fg:/bg: and fg=/bg= prefixes.
        var remaining = text[start..];
        if (StartsWithRgbToken(remaining))
        {
            var closeParen = remaining.IndexOf(')');
            if (closeParen >= 0)
            {
                var endExclusive = start + closeParen + 1;
                token = text.Slice(start, endExclusive - start);
                index = endExclusive;
                return true;
            }
        }

        while (index < text.Length && !IsWhitespace(text[index]))
        {
            index++;
        }

        token = text.Slice(start, index - start);
        return true;
    }

    private static bool StartsWithRgbToken(ReadOnlySpan<char> text)
    {
        return text.StartsWith("rgb(".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               text.StartsWith("fg:rgb(".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               text.StartsWith("fg=rgb(".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               text.StartsWith("bg:rgb(".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               text.StartsWith("bg=rgb(".AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    private static ReadOnlySpan<char> Trim(ReadOnlySpan<char> text)
    {
        var start = 0;
        while (start < text.Length && IsWhitespace(text[start]))
        {
            start++;
        }

        var end = text.Length - 1;
        while (end >= start && IsWhitespace(text[end]))
        {
            end--;
        }

        return text.Slice(start, (end - start) + 1);
    }

    private static ReadOnlySpan<char> TrimSeparators(ReadOnlySpan<char> text)
    {
        var start = 0;
        while (start < text.Length && (text[start] == '-' || text[start] == '_' || text[start] == ':'))
        {
            start++;
        }
        return text[start..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWhitespace(char c) => c == ' ' || c == '\t' || c == '\r' || c == '\n';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AsciiEqualsIgnoreCase(ReadOnlySpan<char> token, string lowerAsciiLiteral) => token.Equals(lowerAsciiLiteral.AsSpan(), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AsciiStartsWithIgnoreCase(ReadOnlySpan<char> token, string lowerAsciiLiteral) => token.StartsWith(lowerAsciiLiteral.AsSpan(), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char ToLowerAsciiInvariant(char c) => (uint)(c - 'A') <= ('Z' - 'A') ? (char)(c | 0x20) : c;
}
