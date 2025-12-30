// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Describes which ANSI/VT features are available and should be used when emitting escape sequences.
/// </summary>
/// <remarks>
/// Terminals and hosts differ in what they support. For example, some environments support only 16 colors,
/// some support 256-color indexed palettes, and some support truecolor (24-bit RGB). Some hosts also disable
/// ANSI interpretation entirely.
/// </remarks>
public record AnsiCapabilities
{
    /// <summary>
    /// Gets a set of defaults intended for modern terminals.
    /// </summary>
    public static readonly AnsiCapabilities Default = new()
    {
        AnsiEnabled = true,
        ColorLevel = AnsiColorLevel.TrueColor,
        SupportsOsc8 = true,
        Prefer7BitC1 = true,
        SafeMode = false,
        OscTermination = AnsiOscTermination.StringTerminator,
    };

    /// <summary>
    /// Gets a value indicating whether ANSI/VT escape sequences should be emitted at all.
    /// </summary>
    public bool AnsiEnabled { get; init; }

    /// <summary>
    /// Gets the maximum color feature level the output target is expected to support.
    /// </summary>
    public AnsiColorLevel ColorLevel { get; init; }

    /// <summary>
    /// Gets a value indicating whether OSC 8 hyperlinks should be emitted.
    /// </summary>
    public bool SupportsOsc8 { get; init; }

    /// <summary>
    /// Gets a value indicating whether the output should prefer 7-bit escape sequences (e.g. <c>ESC [</c>)
    /// instead of 8-bit C1 control codes (e.g. CSI as a single byte).
    /// </summary>
    /// <remarks>
    /// This library currently emits 7-bit sequences only; this option exists to keep the capability model explicit.
    /// </remarks>
    public bool Prefer7BitC1 { get; init; }

    /// <summary>
    /// Gets a value indicating whether the writer should prefer compatibility over minimal output.
    /// </summary>
    /// <remarks>
    /// When enabled, the writer emits "safer" sequences (typically resetting more aggressively) at the cost of
    /// increased escape sequence verbosity.
    /// </remarks>
    public bool SafeMode { get; init; }

    /// <summary>
    /// Gets the preferred terminator to use when emitting OSC strings.
    /// </summary>
    public AnsiOscTermination OscTermination { get; init; }
}
