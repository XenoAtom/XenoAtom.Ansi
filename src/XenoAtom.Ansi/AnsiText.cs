// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using System.Buffers;
using Wcwidth;
using XenoAtom.Ansi.Tokens;

namespace XenoAtom.Ansi;

/// <summary>
/// Provides ANSI-aware text utilities for rich output (strip, measure, wrap, truncate).
/// </summary>
/// <remarks>
/// These helpers treat ANSI escape sequences as zero-width and operate on the visible text.
/// Width measurement uses the <c>Wcwidth</c> algorithm (via the Wcwidth NuGet package).
/// </remarks>
public static class AnsiText
{
    /// <summary>
    /// Removes ANSI/VT escape sequences from the input and returns plain text.
    /// </summary>
    /// <param name="text">The source text, possibly containing ANSI sequences.</param>
    public static string Strip(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return string.Empty;
        }

        using var tokenizer = new AnsiTokenizer();
        var tokens = tokenizer.Tokenize(text, isFinalChunk: true);

        using var builder = new AnsiBuilder(text.Length);
        foreach (var token in tokens)
        {
            switch (token)
            {
                case TextToken t:
                    builder.Append(t.Text);
                    break;
                case ControlToken c:
                    builder.Append(new string(c.Control, 1));
                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Gets the maximum visible width (in terminal cells) across all lines in the text.
    /// </summary>
    /// <param name="text">The source text, possibly containing ANSI sequences.</param>
    /// <param name="tabWidth">Width, in cells, to count for tab characters.</param>
    public static int GetVisibleWidth(ReadOnlySpan<char> text, int tabWidth = 4)
    {
        if (text.IsEmpty)
        {
            return 0;
        }

        if (tabWidth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tabWidth));
        }

        using var tokenizer = new AnsiTokenizer();
        var tokens = tokenizer.Tokenize(text, isFinalChunk: true);

        var currentLine = 0;
        var max = 0;

        foreach (var token in tokens)
        {
            if (token is TextToken t)
            {
                currentLine = AddTextWidth(t.Text.AsSpan(), currentLine, ref max, tabWidth);
            }
            else if (token is ControlToken c)
            {
                switch (c.Control)
                {
                    case '\n':
                        if (currentLine > max) max = currentLine;
                        currentLine = 0;
                        break;
                    case '\r':
                        currentLine = 0;
                        break;
                    case '\t':
                        currentLine += tabWidth;
                        break;
                }
            }
        }

        if (currentLine > max) max = currentLine;
        return max;
    }

    /// <summary>
    /// Truncates text to the specified visible width, optionally preserving ANSI styling.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <param name="width">Maximum visible width in terminal cells.</param>
    /// <param name="ellipsis">Text appended when truncation occurs.</param>
    /// <param name="preserveAnsi">
    /// <see langword="true"/> to preserve styles/hyperlinks while truncating; <see langword="false"/> to return plain text.
    /// </param>
    /// <param name="tabWidth">Width, in cells, to count for tab characters.</param>
    public static string Truncate(string text, int width, string? ellipsis = "…", bool preserveAnsi = true, int tabWidth = 4)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (width <= 0)
        {
            return string.Empty;
        }

        var ellipsisWidth = string.IsNullOrEmpty(ellipsis) ? 0 : GetVisibleWidth(ellipsis.AsSpan(), tabWidth);
        if (ellipsisWidth > width)
        {
            return string.Empty;
        }

        var totalWidth = GetVisibleWidth(text.AsSpan(), tabWidth);
        if (totalWidth <= width)
        {
            return preserveAnsi ? text : Strip(text.AsSpan());
        }

        var limit = width - ellipsisWidth;

        using var parser = new AnsiStyledTextParser();
        var runs = parser.Parse(text.AsSpan());

        if (!preserveAnsi)
        {
            using var plain = new AnsiBuilder(Math.Min(text.Length, width));

            var remaining = limit;
            foreach (var run in runs)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var slice = TakeByWidth(run.Text.AsSpan(), remaining, tabWidth, out var used);
                if (slice.Length > 0)
                {
                    plain.Append(slice);
                }
                remaining -= used;
            }

            if (ellipsisWidth > 0)
            {
                plain.Append(ellipsis!);
            }

            return plain.ToString();
        }

        using var builder = new AnsiBuilder(Math.Min(text.Length + 16, 1024));
        var writer = new AnsiWriter(builder);

        var currentStyle = AnsiStyle.Default;
        AnsiHyperlink? currentLink = null;
        var remainingAnsi = limit;

        foreach (var run in runs)
        {
            if (remainingAnsi <= 0)
            {
                break;
            }

            var slice = TakeByWidth(run.Text.AsSpan(), remainingAnsi, tabWidth, out var used);
            if (slice.IsEmpty)
            {
                continue;
            }

            if (run.Hyperlink?.Uri != currentLink?.Uri || run.Hyperlink?.Id != currentLink?.Id)
            {
                if (currentLink is not null)
                {
                    writer.EndLink();
                }
                if (run.Hyperlink is { } link)
                {
                    writer.BeginLink(link.Uri, link.Id);
                }
                currentLink = run.Hyperlink;
            }

            writer.WriteStyleTransition(currentStyle, run.Style);
            currentStyle = run.Style;
            writer.Write(slice);
            remainingAnsi -= used;
        }

        if (ellipsisWidth > 0)
        {
            writer.Write(ellipsis!);
        }

        if (currentLink is not null)
        {
            writer.EndLink();
        }
        writer.Reset();

        return builder.ToString();
    }

    /// <summary>
    /// Wraps text to the specified visible width, optionally preserving ANSI styling.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <param name="width">Wrap width in terminal cells.</param>
    /// <param name="preserveAnsi">
    /// <see langword="true"/> to preserve styles/hyperlinks across lines; <see langword="false"/> to return plain text lines.
    /// </param>
    /// <param name="tabWidth">Width, in cells, to count for tab characters.</param>
    public static IReadOnlyList<string> Wrap(string text, int width, bool preserveAnsi = true, int tabWidth = 4)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (width <= 0)
        {
            return [];
        }

        using var parser = new AnsiStyledTextParser();
        var runs = parser.Parse(text.AsSpan());
        var lines = new List<List<AnsiStyledRun>>();
        var currentLine = new List<AnsiStyledRun>();
        var currentWidth = 0;

        void CommitLine()
        {
            lines.Add(currentLine);
            currentLine = new List<AnsiStyledRun>();
            currentWidth = 0;
        }

        foreach (var run in runs)
        {
            var remainingText = run.Text.AsSpan();
            while (!remainingText.IsEmpty)
            {
                var newlineIndex = remainingText.IndexOf('\n');
                ReadOnlySpan<char> segment;
                bool hadNewline = false;
                if (newlineIndex >= 0)
                {
                    segment = remainingText[..newlineIndex];
                    hadNewline = true;
                }
                else
                {
                    segment = remainingText;
                }

                while (!segment.IsEmpty)
                {
                    var available = width - currentWidth;
                    if (available <= 0)
                    {
                        CommitLine();
                        available = width;
                    }

                    var slice = TakeByWidth(segment, available, tabWidth, out var used);
                    currentLine.Add(new AnsiStyledRun(slice.ToString(), run.Style, run.Hyperlink));
                    currentWidth += used;
                    segment = segment[slice.Length..];

                    if (!segment.IsEmpty)
                    {
                        CommitLine();
                    }
                }

                if (hadNewline)
                {
                    CommitLine();
                    remainingText = remainingText[(newlineIndex + 1)..];
                }
                else
                {
                    remainingText = ReadOnlySpan<char>.Empty;
                }
            }
        }

        if (currentLine.Count > 0 || lines.Count == 0)
        {
            lines.Add(currentLine);
        }

        var result = new List<string>(lines.Count);
        foreach (var line in lines)
        {
            result.Add(RenderRuns(line, preserveAnsi));
        }

        return result;
    }

    private static int AddTextWidth(ReadOnlySpan<char> text, int currentLine, ref int max, int tabWidth)
    {
        var i = 0;
        while (i < text.Length)
        {
            DecodeRune(text.Slice(i), out var rune, out var consumed);

            i += consumed;

            if (rune.Value == '\n')
            {
                if (currentLine > max) max = currentLine;
                currentLine = 0;
                continue;
            }

            if (rune.Value == '\r')
            {
                currentLine = 0;
                continue;
            }

            if (rune.Value == '\t')
            {
                currentLine += tabWidth;
                continue;
            }

            currentLine += GetRuneWidth(rune);
        }

        return currentLine;
    }

    private static ReadOnlySpan<char> TakeByWidth(ReadOnlySpan<char> text, int width, int tabWidth, out int usedWidth)
    {
        usedWidth = 0;
        if (text.IsEmpty || width <= 0)
        {
            return ReadOnlySpan<char>.Empty;
        }

        var i = 0;
        while (i < text.Length)
        {
            DecodeRune(text.Slice(i), out var rune, out var consumed);

            var w = rune.Value switch
            {
                '\t' => tabWidth,
                '\r' => 0,
                '\n' => 0,
                _ => GetRuneWidth(rune),
            };
            if (usedWidth + w > width)
            {
                break;
            }

            usedWidth += w;
            i += consumed;
        }

        return text[..i];
    }

    private static int GetRuneWidth(Rune rune)
    {
        // Wcwidth returns -1 for non-printable/control, 0 for combining, 1/2 for narrow/wide.
        var w = UnicodeCalculator.GetWidth(rune);
        return w > 0 ? w : 0;
    }

    private static void DecodeRune(ReadOnlySpan<char> text, out Rune rune, out int consumed)
    {
        var status = Rune.DecodeFromUtf16(text, out rune, out consumed);
        if (status == OperationStatus.Done)
        {
            return;
        }

        rune = Rune.ReplacementChar;
        consumed = 1;
    }

    private static string RenderRuns(IReadOnlyList<AnsiStyledRun> runs, bool preserveAnsi)
    {
        if (!preserveAnsi)
        {
            using var sb = new AnsiBuilder();
            foreach (var run in runs)
            {
                sb.Append(run.Text);
            }
            return sb.ToString();
        }

        using var builder = new AnsiBuilder();
        var writer = new AnsiWriter(builder);

        var currentStyle = AnsiStyle.Default;
        AnsiHyperlink? currentLink = null;

        foreach (var run in runs)
        {
            if (run.Hyperlink?.Uri != currentLink?.Uri || run.Hyperlink?.Id != currentLink?.Id)
            {
                if (currentLink is not null)
                {
                    writer.EndLink();
                }
                if (run.Hyperlink is { } link)
                {
                    writer.BeginLink(link.Uri, link.Id);
                }
                currentLink = run.Hyperlink;
            }

            writer.WriteStyleTransition(currentStyle, run.Style);
            currentStyle = run.Style;
            writer.Write(run.Text);
        }

        if (currentLink is not null)
        {
            writer.EndLink();
        }
        writer.Reset();

        return builder.ToString();
    }
}
