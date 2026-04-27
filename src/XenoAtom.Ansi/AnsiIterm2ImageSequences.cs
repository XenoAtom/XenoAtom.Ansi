// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Low-level serialization helpers for iTerm2 inline image OSC 1337 File commands.
/// </summary>
/// <remarks>
/// These helpers only write protocol framing around already-prepared parameters and base64 payload data.
/// They do not decode, resize, encode, or retain images.
/// </remarks>
public static class AnsiIterm2ImageSequences
{
    /// <summary>
    /// Writes an iTerm2 inline image File command using the writer's configured OSC terminator.
    /// </summary>
    /// <param name="writer">The ANSI writer.</param>
    /// <param name="parameters">The File parameters after <c>File=</c>, for example <c>inline=1;width=10;height=5</c>.</param>
    /// <param name="base64Payload">The base64 file payload after the header colon.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="parameters"/> or <paramref name="base64Payload"/> contains unsafe control/framing characters.</exception>
    public static void WriteFile(AnsiWriter writer, ReadOnlySpan<char> parameters, ReadOnlySpan<char> base64Payload)
    {
        ArgumentNullException.ThrowIfNull(writer);
        WriteFile(writer, parameters, base64Payload, writer.Capabilities.OscTermination);
    }

    /// <summary>
    /// Writes an iTerm2 inline image File command using the specified OSC terminator.
    /// </summary>
    /// <param name="writer">The ANSI writer.</param>
    /// <param name="parameters">The File parameters after <c>File=</c>, for example <c>inline=1;width=10;height=5</c>.</param>
    /// <param name="base64Payload">The base64 file payload after the header colon.</param>
    /// <param name="terminator">The OSC terminator to emit.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="parameters"/> or <paramref name="base64Payload"/> contains unsafe control/framing characters.</exception>
    public static void WriteFile(AnsiWriter writer, ReadOnlySpan<char> parameters, ReadOnlySpan<char> base64Payload, AnsiOscTermination terminator)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (!writer.Capabilities.AnsiEnabled)
        {
            return;
        }

        AnsiStringControlValidation.ThrowIfUnsafeParameters(parameters, nameof(parameters), ":");
        AnsiStringControlValidation.ThrowIfUnsafePayload(base64Payload, nameof(base64Payload));

        writer.Write("\x1b]1337;File=");
        writer.Write(parameters);
        writer.Write(":");
        writer.Write(base64Payload);
        writer.WriteOscTerminatorCore(terminator);
    }
}