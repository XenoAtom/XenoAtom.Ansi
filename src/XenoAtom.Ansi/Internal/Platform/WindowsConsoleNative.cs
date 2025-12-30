// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;

namespace XenoAtom.Ansi.Internal.Platform;

internal static class WindowsConsoleNative
{
    private const int StdOutputHandle = -11;

    public static bool TryGetStdOutputHandle(out nint handle)
    {
        handle = GetStdHandle(StdOutputHandle);
        return handle != 0 && handle != -1;
    }

    public static bool TryGetConsoleMode(nint handle, out int mode) => GetConsoleMode(handle, out mode);

    public static bool TrySetConsoleMode(nint handle, int mode) => SetConsoleMode(handle, mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleMode(nint hConsoleHandle, out int lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleMode(nint hConsoleHandle, int dwMode);
}
