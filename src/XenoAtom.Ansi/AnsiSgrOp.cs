// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Identifies a decoded SGR operation.
/// </summary>
public enum AnsiSgrOpKind
{
    /// <summary>
    /// Reset all attributes (SGR 0).
    /// </summary>
    Reset = 0,

    /// <summary>
    /// Set foreground color.
    /// </summary>
    SetForeground = 1,

    /// <summary>
    /// Set background color.
    /// </summary>
    SetBackground = 2,

    /// <summary>
    /// Enable or disable a decoration flag.
    /// </summary>
    SetDecoration = 3,
}

/// <summary>
/// Represents a decoded SGR (Select Graphic Rendition) operation.
/// </summary>
/// <remarks>
/// SGR is the subset of CSI sequences whose final byte is <c>m</c>.
/// This type intentionally models only the operations required by a document renderer.
/// </remarks>
public readonly record struct AnsiSgrOp
{
    private AnsiSgrOp(AnsiSgrOpKind kind, AnsiColor color, AnsiDecorations decorations, bool enabled)
    {
        Kind = kind;
        Color = color;
        Decorations = decorations;
        Enabled = enabled;
    }

    /// <summary>
    /// Gets the operation kind.
    /// </summary>
    public AnsiSgrOpKind Kind { get; }

    /// <summary>
    /// Gets the color value for <see cref="AnsiSgrOpKind.SetForeground"/> and <see cref="AnsiSgrOpKind.SetBackground"/>.
    /// </summary>
    public AnsiColor Color { get; }

    /// <summary>
    /// Gets the decoration flag(s) affected by <see cref="AnsiSgrOpKind.SetDecoration"/>.
    /// </summary>
    public AnsiDecorations Decorations { get; }

    /// <summary>
    /// Gets whether the decoration(s) are enabled (<see langword="true"/>) or disabled (<see langword="false"/>).
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Creates an SGR reset operation.
    /// </summary>
    public static AnsiSgrOp Reset() => new(AnsiSgrOpKind.Reset, AnsiColor.Default, AnsiDecorations.None, enabled: false);

    /// <summary>
    /// Creates a foreground color operation.
    /// </summary>
    public static AnsiSgrOp SetForeground(AnsiColor color) => new(AnsiSgrOpKind.SetForeground, color, AnsiDecorations.None, enabled: false);

    /// <summary>
    /// Creates a background color operation.
    /// </summary>
    public static AnsiSgrOp SetBackground(AnsiColor color) => new(AnsiSgrOpKind.SetBackground, color, AnsiDecorations.None, enabled: false);

    /// <summary>
    /// Creates a decoration enable/disable operation.
    /// </summary>
    public static AnsiSgrOp SetDecoration(AnsiDecorations decoration, bool enabled) => new(AnsiSgrOpKind.SetDecoration, AnsiColor.Default, decoration, enabled);
}
