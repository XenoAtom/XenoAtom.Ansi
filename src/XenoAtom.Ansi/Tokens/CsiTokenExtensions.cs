// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;

namespace XenoAtom.Ansi.Tokens;

/// <summary>
/// Provides convenience helpers for interpreting <see cref="CsiToken"/> instances.
/// </summary>
public static class CsiTokenExtensions
{
    /// <summary>
    /// Attempts to interpret this token as a DECSCUSR cursor style sequence (<c>ESC [ n SP q</c>).
    /// </summary>
    /// <param name="token">The token to inspect.</param>
    /// <param name="style">The parsed cursor style.</param>
    /// <returns><see langword="true"/> if the token is a cursor style sequence and the value is in-range; otherwise <see langword="false"/>.</returns>
    public static bool TryGetCursorStyle(this CsiToken token, out AnsiCursorStyle style)
    {
        style = AnsiCursorStyle.Default;

        if (token.Final != 'q' ||
            token.PrivateMarker is not null ||
            token.Intermediates.Length != 1 ||
            token.Intermediates[0] != ' ')
        {
            return false;
        }

        var parameters = token.Parameters;
        if (parameters.Length == 0)
        {
            return true;
        }

        if (parameters.Length != 1)
        {
            return false;
        }

        var raw = parameters[0];
        if ((uint)raw > 6u)
        {
            return false;
        }

        style = (AnsiCursorStyle)raw;
        return true;
    }
}

