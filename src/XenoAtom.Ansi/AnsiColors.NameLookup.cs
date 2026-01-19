// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

public static partial class AnsiColors
{
    private static readonly Dictionary<string, AnsiColor> s_webByName = CreateWebByName();
    private static readonly Dictionary<string, AnsiColor>.AlternateLookup<ReadOnlySpan<char>> s_webByNameSpan =
        s_webByName.GetAlternateLookup<ReadOnlySpan<char>>();

    /// <summary>
    /// Attempts to get a color by name.
    /// </summary>
    /// <remarks>
    /// This method recognizes:
    /// <list type="bullet">
    /// <item><description>Basic-16 names (<c>red</c>, <c>brightred</c>, <c>light-blue</c>, etc.)</description></item>
    /// <item><description>Basic synonyms (<c>purple</c> = <c>magenta</c>, <c>aqua</c> = <c>cyan</c>, <c>gray</c>/<c>grey</c> = bright black)</description></item>
    /// <item><description>Web (CSS/SVG/X11) named colors (<c>cornflowerblue</c>, <c>rebeccapurple</c>, ...)</description></item>
    /// </list>
    /// Use the <c>web:</c> prefix to force web colors when there is ambiguity (e.g. <c>web:gray</c>, <c>web:red</c>).
    /// </remarks>
    public static bool TryGetByName(ReadOnlySpan<char> name, out AnsiColor color)
    {
        if (name.IsEmpty)
        {
            color = default;
            return false;
        }

        if (TryStripPrefix(name, "web:", out var webName))
        {
            return TryGetWebByName(webName, out color);
        }

        if (TryGetBasicOrSynonymByName(name, out color))
        {
            return true;
        }

        if (TryGetBasicOrSynonymByNameWithBrightPrefix(name, out color))
        {
            return true;
        }

        return TryGetWebByName(name, out color);
    }

    /// <summary>
    /// Attempts to get a Web (CSS/SVG/X11) named color by name.
    /// </summary>
    public static bool TryGetWebByName(ReadOnlySpan<char> name, out AnsiColor color)
    {
        if (name.IsEmpty)
        {
            color = default;
            return false;
        }

        if (s_webByNameSpan.TryGetValue(name, out color))
        {
            return true;
        }

        if (!ContainsSeparators(name))
        {
            return false;
        }

        Span<char> normalizedBuffer = stackalloc char[name.Length];
        var normalized = RemoveSeparators(name, normalizedBuffer);
        if (normalized.IsEmpty)
        {
            color = default;
            return false;
        }

        return s_webByNameSpan.TryGetValue(normalized, out color);
    }

    /// <summary>
    /// Attempts to get a color by name.
    /// </summary>
    /// <param name="name">The name of the color.</param>
    /// <param name="color">The resolved color.</param>
    /// <returns><see langword="true"/> if the color name was recognized.</returns>
    public static bool TryGetByName(string? name, out AnsiColor color)
    {
        if (string.IsNullOrEmpty(name))
        {
            color = default;
            return false;
        }

        return TryGetByName(name.AsSpan(), out color);
    }

    private static Dictionary<string, AnsiColor> CreateWebByName()
    {
        var colors = new Dictionary<string, AnsiColor>(StringComparer.OrdinalIgnoreCase);
        AddWebColors(colors);
        return colors;
    }

    private static partial void AddWebColors(Dictionary<string, AnsiColor> colors);

    private static bool TryGetBasicOrSynonymByName(ReadOnlySpan<char> name, out AnsiColor color)
    {
        // Keep AnsiMarkup's historical basic palette behavior for these names.
        switch (ToLowerAsciiInvariant(name[0]))
        {
            case 'd':
                if (AsciiEqualsIgnoreCase(name, "default"))
                {
                    color = AnsiColor.Default;
                    return true;
                }
                break;
            case 'g':
                if (AsciiEqualsIgnoreCase(name, "gray") || AsciiEqualsIgnoreCase(name, "grey"))
                {
                    color = BrightBlack;
                    return true;
                }
                break;
        }

        if (TryGetBasicColorIndex(name, out var baseIndex))
        {
            color = AnsiColor.Basic16(baseIndex);
            return true;
        }

        color = default;
        return false;
    }

    private static bool TryGetBasicOrSynonymByNameWithBrightPrefix(ReadOnlySpan<char> name, out AnsiColor color)
    {
        if (TryStripPrefix(name, "bright", out var remainder))
        {
            name = TrimSeparators(remainder);
        }
        else if (TryStripPrefix(name, "light", out remainder))
        {
            name = TrimSeparators(remainder);
        }
        else
        {
            color = default;
            return false;
        }

        if (name.IsEmpty || !TryGetBasicColorIndex(name, out var baseIndex))
        {
            color = default;
            return false;
        }

        color = AnsiColor.Basic16(baseIndex + 8);
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

    private static ReadOnlySpan<char> TrimSeparators(ReadOnlySpan<char> text)
    {
        var start = 0;
        while (start < text.Length && (text[start] == '-' || text[start] == '_' || text[start] == ':'))
        {
            start++;
        }
        return text[start..];
    }

    private static bool ContainsSeparators(ReadOnlySpan<char> name)
    {
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c == '-' || c == '_' || c == ' ' || c == '\t')
            {
                return true;
            }
        }

        return false;
    }

    private static ReadOnlySpan<char> RemoveSeparators(ReadOnlySpan<char> name, Span<char> destination)
    {
        var length = 0;
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c == '-' || c == '_' || c == ' ' || c == '\t')
            {
                continue;
            }

            destination[length++] = c;
        }

        return destination[..length];
    }

    private static bool TryStripPrefix(ReadOnlySpan<char> text, string prefix, out ReadOnlySpan<char> remainder)
    {
        if (text.StartsWith(prefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            remainder = text[prefix.Length..];
            return true;
        }

        remainder = default;
        return false;
    }

    private static bool AsciiEqualsIgnoreCase(ReadOnlySpan<char> token, string lowerAsciiLiteral) =>
        token.Equals(lowerAsciiLiteral.AsSpan(), StringComparison.OrdinalIgnoreCase);

    private static char ToLowerAsciiInvariant(char c) => (uint)(c - 'A') <= ('Z' - 'A') ? (char)(c | 0x20) : c;
}
