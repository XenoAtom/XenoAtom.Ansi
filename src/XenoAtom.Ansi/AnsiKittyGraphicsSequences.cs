// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Low-level serialization helpers for Kitty graphics protocol APC commands.
/// </summary>
/// <remarks>
/// These helpers only write protocol framing around already-prepared parameters and base64 payload data.
/// They do not decode, resize, encode, or retain images.
/// </remarks>
public static class AnsiKittyGraphicsSequences
{
    /// <summary>
    /// The default maximum payload chunk size, in base64 characters, used by <see cref="WriteCommandChunks(AnsiWriter, ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char})"/>.
    /// </summary>
    public const int DefaultMaxPayloadChunkChars = 4096;

    /// <summary>
    /// Writes a Kitty graphics APC command (<c>ESC _ G ... ST</c>).
    /// </summary>
    /// <param name="writer">The ANSI writer.</param>
    /// <param name="parameters">Comma-separated Kitty graphics parameters, excluding the leading <c>G</c> and trailing semicolon.</param>
    /// <param name="payload">Optional payload after the semicolon, typically base64.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="parameters"/> or <paramref name="payload"/> contains unsafe control/framing characters.</exception>
    public static void WriteCommand(AnsiWriter writer, ReadOnlySpan<char> parameters, ReadOnlySpan<char> payload)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (!writer.Capabilities.AnsiEnabled)
        {
            return;
        }

        AnsiStringControlValidation.ThrowIfUnsafeParameters(parameters, nameof(parameters), ";");
        AnsiStringControlValidation.ThrowIfUnsafePayload(payload, nameof(payload));

        writer.Write("\x1b_G");
        writer.Write(parameters);
        if (!payload.IsEmpty)
        {
            writer.Write(";");
            writer.Write(payload);
        }
        writer.WriteStringTerminatorCore();
    }

    /// <summary>
    /// Writes Kitty graphics APC command chunks using the default 4096-character payload chunk size.
    /// </summary>
    /// <param name="writer">The ANSI writer.</param>
    /// <param name="firstParameters">Parameters for the first chunk. Do not include an <c>m=</c> chunk flag.</param>
    /// <param name="continuationParameters">Parameters for continuation chunks. Do not include an <c>m=</c> chunk flag.</param>
    /// <param name="payload">The payload to split, typically base64.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Parameters or payload contain unsafe control/framing characters.</exception>
    public static void WriteCommandChunks(AnsiWriter writer, ReadOnlySpan<char> firstParameters, ReadOnlySpan<char> continuationParameters, ReadOnlySpan<char> payload) =>
        WriteCommandChunks(writer, firstParameters, continuationParameters, payload, DefaultMaxPayloadChunkChars);

    /// <summary>
    /// Writes Kitty graphics APC command chunks using the specified payload chunk size.
    /// </summary>
    /// <param name="writer">The ANSI writer.</param>
    /// <param name="firstParameters">Parameters for the first chunk. Do not include an <c>m=</c> chunk flag.</param>
    /// <param name="continuationParameters">Parameters for continuation chunks. Do not include an <c>m=</c> chunk flag.</param>
    /// <param name="payload">The payload to split, typically base64.</param>
    /// <param name="maxPayloadChunkChars">Maximum payload characters per chunk. Must be greater than zero.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxPayloadChunkChars"/> is less than one.</exception>
    /// <exception cref="ArgumentException">Parameters or payload contain unsafe control/framing characters.</exception>
    public static void WriteCommandChunks(AnsiWriter writer, ReadOnlySpan<char> firstParameters, ReadOnlySpan<char> continuationParameters, ReadOnlySpan<char> payload, int maxPayloadChunkChars)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPayloadChunkChars, 1);

        if (!writer.Capabilities.AnsiEnabled)
        {
            return;
        }

        AnsiStringControlValidation.ThrowIfUnsafeParameters(firstParameters, nameof(firstParameters), ";");
        AnsiStringControlValidation.ThrowIfUnsafeParameters(continuationParameters, nameof(continuationParameters), ";");
        AnsiStringControlValidation.ThrowIfUnsafePayload(payload, nameof(payload));

        if (payload.IsEmpty)
        {
            WriteChunk(writer, firstParameters, payload, moreChunks: false);
            return;
        }

        var offset = 0;
        var first = true;
        while (offset < payload.Length)
        {
            var count = Math.Min(maxPayloadChunkChars, payload.Length - offset);
            var nextOffset = offset + count;
            WriteChunk(writer, first ? firstParameters : continuationParameters, payload.Slice(offset, count), moreChunks: nextOffset < payload.Length);
            offset = nextOffset;
            first = false;
        }
    }

    /// <summary>
    /// Attempts to parse a Kitty graphics APC reply token.
    /// </summary>
    /// <param name="token">The APC token to parse.</param>
    /// <param name="reply">The parsed reply when successful.</param>
    /// <returns><see langword="true"/> if <paramref name="token"/> contains a syntactically valid Kitty graphics reply.</returns>
    public static bool TryParseReply(Tokens.AnsiStringControlToken token, out AnsiKittyGraphicsReply reply)
    {
        if (token.Kind != AnsiStringControlKind.Apc)
        {
            reply = default;
            return false;
        }

        return TryParseReply(token.Data.AsSpan(), out reply);
    }

    /// <summary>
    /// Attempts to parse Kitty graphics APC reply data, excluding the APC introducer and ST terminator.
    /// </summary>
    /// <param name="data">The APC data. It must start with <c>G</c>.</param>
    /// <param name="reply">The parsed reply when successful.</param>
    /// <returns><see langword="true"/> if <paramref name="data"/> is syntactically valid Kitty graphics reply data.</returns>
    public static bool TryParseReply(ReadOnlySpan<char> data, out AnsiKittyGraphicsReply reply)
    {
        reply = default;
        if (data.Length < 3 || data[0] != 'G')
        {
            return false;
        }

        data = data[1..];
        var semicolonIndex = data.IndexOf(';');
        if (semicolonIndex < 0 || semicolonIndex == data.Length - 1)
        {
            return false;
        }

        var parameterSpan = data[..semicolonIndex];
        var responseSpan = data[(semicolonIndex + 1)..];
        var messageIndex = responseSpan.IndexOf(':');
        var statusSpan = messageIndex < 0 ? responseSpan : responseSpan[..messageIndex];
        if (statusSpan.IsEmpty)
        {
            return false;
        }

        var message = messageIndex < 0 || messageIndex == responseSpan.Length - 1 ? null : responseSpan[(messageIndex + 1)..].ToString();
        reply = new AnsiKittyGraphicsReply(
            TryGetIntParameter(parameterSpan, "i", out var imageId) ? imageId : null,
            TryGetIntParameter(parameterSpan, "p", out var placementId) ? placementId : null,
            statusSpan.ToString(),
            parameterSpan.ToString(),
            message);
        return true;
    }

    private static void WriteChunk(AnsiWriter writer, ReadOnlySpan<char> parameters, ReadOnlySpan<char> payload, bool moreChunks)
    {
        writer.Write("\x1b_G");
        writer.Write(parameters);
        if (!parameters.IsEmpty)
        {
            writer.Write(",");
        }
        writer.Write(moreChunks ? "m=1;" : "m=0;");
        writer.Write(payload);
        writer.WriteStringTerminatorCore();
    }

    private static bool TryGetIntParameter(ReadOnlySpan<char> parameters, ReadOnlySpan<char> name, out int value)
    {
        value = 0;
        var start = 0;
        while (start <= parameters.Length)
        {
            var comma = parameters[start..].IndexOf(',');
            var end = comma < 0 ? parameters.Length : start + comma;
            var part = parameters[start..end];
            var equals = part.IndexOf('=');
            if (equals > 0 && part[..equals].Equals(name, StringComparison.Ordinal))
            {
                return TryParseInt(part[(equals + 1)..], out value);
            }

            if (comma < 0)
            {
                break;
            }
            start = end + 1;
        }

        return false;
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
}