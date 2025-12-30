// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi.Tokens;

namespace XenoAtom.Ansi;

/// <summary>
/// Converts ANSI text into styled runs by interpreting SGR (style changes) and OSC 8 (hyperlinks).
/// </summary>
/// <remarks>
/// This is a convenience wrapper around <see cref="AnsiTokenizer"/> intended for UI/rendering scenarios.
/// It does not emulate terminal cursor movement; it only produces runs in source order.
/// </remarks>
public sealed class AnsiStyledTextParser : IDisposable
{
    private readonly AnsiTokenizer _tokenizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnsiStyledTextParser"/> class.
    /// </summary>
    /// <param name="tokenizerOptions">Options passed to the underlying tokenizer.</param>
    public AnsiStyledTextParser(AnsiTokenizerOptions tokenizerOptions = default)
    {
        _tokenizer = new AnsiTokenizer(tokenizerOptions);
    }

    /// <summary>
    /// Parses a complete input string into styled runs.
    /// </summary>
    public List<AnsiStyledRun> Parse(ReadOnlySpan<char> text) => Parse(text, isFinalChunk: true);

    /// <summary>
    /// Parses a chunk into styled runs.
    /// </summary>
    /// <param name="chunk">The input chunk.</param>
    /// <param name="isFinalChunk">Whether this is the final chunk.</param>
    public List<AnsiStyledRun> Parse(ReadOnlySpan<char> chunk, bool isFinalChunk)
    {
        var tokens = _tokenizer.Tokenize(chunk, isFinalChunk);
        var runs = new List<AnsiStyledRun>(tokens.Count);

        var style = AnsiStyle.Default;
        AnsiHyperlink? hyperlink = null;

        foreach (var token in tokens)
        {
            switch (token)
            {
                case TextToken t:
                    if (!string.IsNullOrEmpty(t.Text))
                    {
                        runs.Add(new AnsiStyledRun(t.Text, style, hyperlink));
                    }
                    break;
                case ControlToken c:
                    runs.Add(new AnsiStyledRun(new string(c.Control, 1), style, hyperlink));
                    break;
                case SgrToken sgr:
                    ApplySgr(ref style, sgr.Operations);
                    break;
                case OscToken osc:
                    if (osc.Code == 8)
                    {
                        hyperlink = TryParseOsc8(osc.Data, out var link) ? link : hyperlink;
                    }
                    break;
            }
        }

        return runs;
    }

    /// <inheritdoc />
    public void Dispose() => _tokenizer.Dispose();

    private static void ApplySgr(ref AnsiStyle style, AnsiSgrOp[] ops)
    {
        foreach (var op in ops)
        {
            switch (op.Kind)
            {
                case AnsiSgrOpKind.Reset:
                    style = AnsiStyle.Default;
                    break;
                case AnsiSgrOpKind.SetForeground:
                    style = style with { Foreground = op.Color };
                    break;
                case AnsiSgrOpKind.SetBackground:
                    style = style with { Background = op.Color };
                    break;
                case AnsiSgrOpKind.SetDecoration:
                    style = style with
                    {
                        Decorations = op.Enabled
                            ? style.Decorations | op.Decorations
                            : style.Decorations & ~op.Decorations,
                    };
                    break;
            }
        }
    }

    private static bool TryParseOsc8(string data, out AnsiHyperlink? hyperlink)
    {
        // data: params ; uri
        // End link: params="" and uri=""
        hyperlink = null;

        var sepIndex = data.IndexOf(';');
        if (sepIndex < 0)
        {
            return false;
        }

        var paramsPart = data[..sepIndex];
        var uriPart = data[(sepIndex + 1)..];

        if (string.IsNullOrEmpty(uriPart))
        {
            hyperlink = null;
            return true;
        }

        string? id = null;
        if (!string.IsNullOrEmpty(paramsPart))
        {
            // Common form: id=...
            const string idPrefix = "id=";
            var idIndex = paramsPart.IndexOf(idPrefix, StringComparison.Ordinal);
            if (idIndex >= 0)
            {
                id = paramsPart[(idIndex + idPrefix.Length)..];
            }
        }

        hyperlink = new AnsiHyperlink(uriPart, id);
        return true;
    }
}
