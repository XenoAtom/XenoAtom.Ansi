// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Options controlling tokenization behavior and safety limits.
/// </summary>
/// <remarks>
/// ANSI/VT sequences can appear in untrusted logs. These limits help prevent unbounded memory growth
/// when encountering malformed or never-terminated sequences (especially OSC/DCS strings).
/// </remarks>
public readonly record struct AnsiTokenizerOptions
{
    /// <summary>
    /// Gets a default set of options suitable for typical usage.
    /// </summary>
    public static readonly AnsiTokenizerOptions Default = new()
    {
        DecodeSgr = true,
        MaxOscLength = 64 * 1024,
        MaxEscapeSequenceLength = 64 * 1024,
        MaxTokenCountPerChunk = 16 * 1024,
    };

    /// <summary>
    /// Gets a value indicating whether CSI <c>... m</c> sequences should be decoded into <see cref="AnsiSgrOp"/> operations.
    /// </summary>
    public bool DecodeSgr { get; init; }

    /// <summary>
    /// Gets the maximum number of characters to buffer for a single OSC string before giving up.
    /// </summary>
    public int MaxOscLength { get; init; }

    /// <summary>
    /// Gets the maximum number of characters to buffer for a single escape sequence (CSI, DCS, etc.) before giving up.
    /// </summary>
    public int MaxEscapeSequenceLength { get; init; }

    /// <summary>
    /// Gets the maximum number of tokens emitted per chunk to prevent pathological inputs from producing huge token lists.
    /// </summary>
    public int MaxTokenCountPerChunk { get; init; }
}
