// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

internal static class AnsiStringControlValidation
{
    public static void ThrowIfUnsafePayload(ReadOnlySpan<char> payload, string paramName)
    {
        for (var i = 0; i < payload.Length; i++)
        {
            if (IsControlOrDelete(payload[i]))
            {
                throw new ArgumentException("Terminal string control payloads cannot contain C0/C1 control characters or DEL.", paramName);
            }
        }
    }

    public static void ThrowIfUnsafeParameters(ReadOnlySpan<char> parameters, string paramName, ReadOnlySpan<char> additionalDisallowed)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            var c = parameters[i];
            if (IsControlOrDelete(c) || additionalDisallowed.Contains(c))
            {
                throw new ArgumentException("Terminal string control parameters contain a character that would change the protocol framing.", paramName);
            }
        }
    }

    private static bool IsControlOrDelete(char c) => c <= '\x1f' || c == '\x7f' || (c >= '\x80' && c <= '\x9f');
}