// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;
using XenoAtom.Ansi.Internal.Platform;

namespace XenoAtom.Ansi.Platform;

/// <summary>
/// Indicates the result of attempting to enable Windows console Virtual Terminal processing.
/// </summary>
public enum WindowsVirtualTerminalResultKind
{
    /// <summary>
    /// Virtual Terminal processing was enabled by this call.
    /// </summary>
    Enabled = 0,

    /// <summary>
    /// Virtual Terminal processing was already enabled.
    /// </summary>
    AlreadyEnabled = 1,

    /// <summary>
    /// The operation is not supported (non-Windows or not a console).
    /// </summary>
    NotSupported = 2,

    /// <summary>
    /// The operation failed; see <see cref="WindowsVirtualTerminalResult.ErrorCode"/>.
    /// </summary>
    Failed = 3,
}

/// <summary>
/// Result returned by <see cref="WindowsConsole.TryEnableVirtualTerminalProcessing"/>.
/// </summary>
/// <param name="Kind">The result kind.</param>
/// <param name="ErrorCode">The Win32 error code when <paramref name="Kind"/> is <see cref="WindowsVirtualTerminalResultKind.Failed"/>.</param>
public readonly record struct WindowsVirtualTerminalResult(WindowsVirtualTerminalResultKind Kind, int ErrorCode = 0)
{
    /// <summary>Creates an <see cref="WindowsVirtualTerminalResultKind.Enabled"/> result.</summary>
    public static WindowsVirtualTerminalResult Enabled() => new(WindowsVirtualTerminalResultKind.Enabled);

    /// <summary>Creates an <see cref="WindowsVirtualTerminalResultKind.AlreadyEnabled"/> result.</summary>
    public static WindowsVirtualTerminalResult AlreadyEnabled() => new(WindowsVirtualTerminalResultKind.AlreadyEnabled);

    /// <summary>Creates an <see cref="WindowsVirtualTerminalResultKind.NotSupported"/> result.</summary>
    public static WindowsVirtualTerminalResult NotSupported() => new(WindowsVirtualTerminalResultKind.NotSupported);

    /// <summary>Creates a <see cref="WindowsVirtualTerminalResultKind.Failed"/> result.</summary>
    public static WindowsVirtualTerminalResult Failed(int errorCode) => new(WindowsVirtualTerminalResultKind.Failed, errorCode);
}

/// <summary>
/// Windows console helpers.
/// </summary>
public static class WindowsConsole
{
    /// <summary>
    /// Attempts to enable Virtual Terminal processing for the current process standard output console.
    /// </summary>
    /// <remarks>
    /// On Windows, enabling <c>ENABLE_VIRTUAL_TERMINAL_PROCESSING</c> allows ANSI/VT escape sequences to be interpreted
    /// by the console host. This method is opt-in and has side effects (it changes the console mode).
    /// </remarks>
    public static WindowsVirtualTerminalResult TryEnableVirtualTerminalProcessing()
    {
        if (!OperatingSystem.IsWindows())
        {
            return WindowsVirtualTerminalResult.NotSupported();
        }

        if (!WindowsConsoleNative.TryGetStdOutputHandle(out var handle))
        {
            return WindowsVirtualTerminalResult.Failed(Marshal.GetLastWin32Error());
        }

        if (!WindowsConsoleNative.TryGetConsoleMode(handle, out var mode))
        {
            return WindowsVirtualTerminalResult.Failed(Marshal.GetLastWin32Error());
        }

        const int enableVirtualTerminalProcessing = 0x0004;
        if ((mode & enableVirtualTerminalProcessing) != 0)
        {
            return WindowsVirtualTerminalResult.AlreadyEnabled();
        }

        var newMode = mode | enableVirtualTerminalProcessing;
        if (!WindowsConsoleNative.TrySetConsoleMode(handle, newMode))
        {
            return WindowsVirtualTerminalResult.Failed(Marshal.GetLastWin32Error());
        }

        return WindowsVirtualTerminalResult.Enabled();
    }
}
