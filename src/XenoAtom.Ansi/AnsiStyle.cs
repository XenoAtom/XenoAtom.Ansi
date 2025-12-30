// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Represents a terminal text style that can be encoded/decoded via SGR (Select Graphic Rendition).
/// </summary>
/// <remarks>
/// This is a minimal model intended for rich-output renderers. It does not attempt to model the full terminal state.
/// Null foreground/background indicates "no change / inherit" when used in transitions.
/// </remarks>
public readonly record struct AnsiStyle
{
    /// <summary>
    /// The default style (no decorations, default foreground/background).
    /// </summary>
    public static readonly AnsiStyle Default = new()
    {
        Foreground = AnsiColor.Default,
        Background = AnsiColor.Default,
        Decorations = AnsiDecorations.None,
    };

    /// <summary>
    /// Gets the foreground color, or <see langword="null"/> to indicate "unset / inherit".
    /// </summary>
    public AnsiColor? Foreground { get; init; }

    /// <summary>
    /// Gets the background color, or <see langword="null"/> to indicate "unset / inherit".
    /// </summary>
    public AnsiColor? Background { get; init; }

    /// <summary>
    /// Gets the decoration flags.
    /// </summary>
    public AnsiDecorations Decorations { get; init; }

    /// <summary>
    /// Returns a copy of this style with a new foreground color.
    /// </summary>
    public AnsiStyle WithForeground(AnsiColor? color) => this with { Foreground = color };

    /// <summary>
    /// Returns a copy of this style with a new background color.
    /// </summary>
    public AnsiStyle WithBackground(AnsiColor? color) => this with { Background = color };

    /// <summary>
    /// Returns a copy of this style with new decoration flags.
    /// </summary>
    public AnsiStyle WithDecorations(AnsiDecorations decorations) => this with { Decorations = decorations };

    /// <summary>
    /// Resolves <see langword="null"/> foreground/background values from the specified fallback style.
    /// </summary>
    /// <param name="fallback">The fallback to use when a color is <see langword="null"/>.</param>
    public AnsiStyle ResolveMissingFrom(AnsiStyle fallback)
    {
        var foreground = Foreground ?? fallback.Foreground ?? AnsiColor.Default;
        var background = Background ?? fallback.Background ?? AnsiColor.Default;
        return new AnsiStyle { Foreground = foreground, Background = background, Decorations = Decorations };
    }
}
