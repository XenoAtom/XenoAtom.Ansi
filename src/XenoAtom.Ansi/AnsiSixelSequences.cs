// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Low-level serialization helpers for Sixel DCS image payloads.
/// </summary>
/// <remarks>
/// These helpers only write DCS framing around an already-encoded Sixel payload.
/// They do not decode images, generate palettes, quantize pixels, or encode Sixel raster data.
/// </remarks>
public static class AnsiSixelSequences
{
    /// <summary>
    /// Writes a Sixel DCS payload (<c>ESC P q ... ST</c>).
    /// </summary>
    /// <param name="writer">The ANSI writer.</param>
    /// <param name="payload">The already-encoded Sixel payload after the <c>q</c> final byte.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="payload"/> contains unsafe control characters.</exception>
    public static void WriteImage(AnsiWriter writer, ReadOnlySpan<char> payload) => WriteImage(writer, ReadOnlySpan<char>.Empty, payload);

    /// <summary>
    /// Writes a Sixel DCS payload with DCS parameters (<c>ESC P parameters q ... ST</c>).
    /// </summary>
    /// <param name="writer">The ANSI writer.</param>
    /// <param name="parameters">Optional DCS parameters before the <c>q</c> final byte, for example <c>0;1</c>.</param>
    /// <param name="payload">The already-encoded Sixel payload after the <c>q</c> final byte.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="parameters"/> or <paramref name="payload"/> contains unsafe control/framing characters.</exception>
    public static void WriteImage(AnsiWriter writer, ReadOnlySpan<char> parameters, ReadOnlySpan<char> payload)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (!writer.Capabilities.AnsiEnabled)
        {
            return;
        }

        AnsiStringControlValidation.ThrowIfUnsafeParameters(parameters, nameof(parameters), "q");
        AnsiStringControlValidation.ThrowIfUnsafePayload(payload, nameof(payload));

        writer.Write("\x1bP");
        writer.Write(parameters);
        writer.Write("q");
        writer.Write(payload);
        writer.WriteStringTerminatorCore();
    }
}