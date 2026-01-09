// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Abstraction over a writer that can emit styled output.
/// </summary>
/// <remarks>
/// This interface is primarily intended to allow capturing style transitions (e.g. for emitting non-ANSI output)
/// while reusing components such as <see cref="AnsiMarkup"/>.
/// </remarks>
public interface IAnsiBasicWriter
{
    /// <summary>
    /// Gets the capabilities used by this writer.
    /// </summary>
    AnsiCapabilities Capabilities { get; }

    /// <summary>
    /// Writes the specified text verbatim.
    /// </summary>
    /// <param name="text">The text to write.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    void Write(ReadOnlySpan<char> text);

    /// <summary>
    /// Writes the specified text verbatim.
    /// </summary>
    /// <param name="text">The text to write.</param>
    /// <returns>This writer, for fluent chaining.</returns>
    void Write(string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            Write(text.AsSpan());
        }
    }

    /// <summary>
    /// Emits a transition between two styles.
    /// </summary>
    /// <returns>This writer, for fluent chaining.</returns>
    void StyleTransition(AnsiStyle from, AnsiStyle to);
}

