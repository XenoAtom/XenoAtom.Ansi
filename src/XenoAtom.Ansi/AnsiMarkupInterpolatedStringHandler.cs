// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using System.Runtime.CompilerServices;

namespace XenoAtom.Ansi;

/// <summary>
/// Interpolated string handler for <see cref="AnsiMarkup"/> that escapes formatted values to prevent markup injection.
/// </summary>
[InterpolatedStringHandler]
public struct AnsiMarkupInterpolatedStringHandler
{
    private char[]? _buffer;
    private int _length;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnsiMarkupInterpolatedStringHandler"/> struct.
    /// </summary>
    public AnsiMarkupInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        _buffer = ArrayPool<char>.Shared.Rent(Math.Max(32, literalLength));
        _length = 0;
    }

    /// <summary>
    /// Gets the written markup.
    /// </summary>
    public readonly ReadOnlySpan<char> WrittenSpan => _buffer is null ? ReadOnlySpan<char>.Empty : _buffer.AsSpan(0, _length);

    /// <summary>
    /// Appends a literal markup segment (not escaped).
    /// </summary>
    public void AppendLiteral(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        AppendRaw(value.AsSpan());
    }

    /// <summary>
    /// Appends a formatted value, escaping markup brackets.
    /// </summary>
    public void AppendFormatted(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        AppendEscaped(value.AsSpan());
    }

    /// <summary>
    /// Appends a formatted value, escaping markup brackets.
    /// </summary>
    public void AppendFormatted(string? value, int alignment = 0, string? format = null)
    {
        _ = format;
        if (string.IsNullOrEmpty(value))
        {
            AppendAlignment(alignment, 0);
            return;
        }

        AppendWithAlignment(value.AsSpan(), alignment, escape: true);
    }

    /// <summary>
    /// Appends a formatted value, escaping markup brackets.
    /// </summary>
    public void AppendFormatted(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return;
        }

        AppendEscaped(value);
    }

    /// <summary>
    /// Appends a formatted value, escaping markup brackets.
    /// </summary>
    public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
    {
        _ = format;
        AppendWithAlignment(value, alignment, escape: true);
    }

    /// <summary>
    /// Appends a formatted value, escaping markup brackets when needed.
    /// </summary>
    public void AppendFormatted(char value)
    {
        if (value is '[' or ']')
        {
            EnsureCapacity(2);
            _buffer![_length++] = value;
            _buffer![_length++] = value;
            return;
        }

        EnsureCapacity(1);
        _buffer![_length++] = value;
    }

    /// <summary>
    /// Appends a formatted value, escaping markup brackets when needed.
    /// </summary>
    public void AppendFormatted(char value, int alignment = 0, string? format = null)
    {
        _ = format;
        if (alignment == 0)
        {
            AppendFormatted(value);
            return;
        }

        const int logicalLen = 1;
        if (alignment > 0)
        {
            AppendAlignment(alignment, logicalLen);
        }
        AppendFormatted(value);
        if (alignment < 0)
        {
            AppendAlignment(alignment, logicalLen);
        }
    }

    /// <summary>
    /// Appends a formatted value, escaping markup brackets.
    /// </summary>
    public void AppendFormatted<T>(T value) where T : ISpanFormattable
    {
        Span<char> buffer = stackalloc char[64];
        if (value.TryFormat(buffer, out var written, default, provider: null))
        {
            AppendEscaped(buffer[..written]);
            return;
        }

        AppendFormatted(value.ToString(null, formatProvider: null));
    }

    /// <summary>
    /// Appends a formatted value, escaping markup brackets.
    /// </summary>
    public void AppendFormatted<T>(T value, string? format) where T : ISpanFormattable
    {
        Span<char> buffer = stackalloc char[64];
        if (value.TryFormat(buffer, out var written, format, provider: null))
        {
            AppendEscaped(buffer[..written]);
            return;
        }

        AppendFormatted(value.ToString(format, formatProvider: null));
    }

    /// <summary>
    /// Appends a formatted value, escaping markup brackets.
    /// </summary>
    public void AppendFormatted<T>(T value, int alignment, string? format = null) where T : ISpanFormattable
    {
        Span<char> buffer = stackalloc char[64];
        if (!value.TryFormat(buffer, out var written, format, provider: null))
        {
            AppendFormatted(value.ToString(format, formatProvider: null), alignment, format: null);
            return;
        }

        var formatted = buffer[..written];
        AppendWithAlignment(formatted, alignment, escape: true);
    }

    /// <summary>
    /// Appends a formatted value, escaping markup brackets.
    /// </summary>
    public void AppendFormatted(object? value)
    {
        if (value is null)
        {
            return;
        }

        AppendFormatted(value.ToString());
    }

    /// <summary>
    /// Appends a formatted value, escaping markup brackets.
    /// </summary>
    public void AppendFormatted(object? value, int alignment = 0, string? format = null)
    {
        _ = format;
        if (value is null)
        {
            AppendAlignment(alignment, 0);
            return;
        }

        AppendFormatted(value.ToString(), alignment, format: null);
    }

    /// <summary>
    /// Returns the internal pooled buffer to the pool.
    /// </summary>
    public void Dispose()
    {
        if (_buffer is not null)
        {
            ArrayPool<char>.Shared.Return(_buffer);
            _buffer = null;
            _length = 0;
        }
    }

    private void AppendRaw(ReadOnlySpan<char> value)
    {
        EnsureCapacity(value.Length);
        value.CopyTo(_buffer.AsSpan(_length));
        _length += value.Length;
    }

    private void AppendEscaped(ReadOnlySpan<char> value)
    {
        var extra = 0;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == '[' || c == ']')
            {
                extra++;
            }
        }

        if (extra == 0)
        {
            AppendRaw(value);
            return;
        }

        EnsureCapacity(value.Length + extra);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == '[' || c == ']')
            {
                _buffer![_length++] = c;
                _buffer![_length++] = c;
            }
            else
            {
                _buffer![_length++] = c;
            }
        }
    }

    private void AppendWithAlignment(ReadOnlySpan<char> value, int alignment, bool escape)
    {
        if (alignment == 0)
        {
            if (escape)
            {
                AppendEscaped(value);
            }
            else
            {
                AppendRaw(value);
            }
            return;
        }

        // Alignment should apply to the logical formatted value, not the escaped representation.
        // Escaping may increase the intermediate markup length (e.g. "[" -> "[["), but the final rendered text
        // should still be aligned based on the original formatted length.
        var width = value.Length;
        if (alignment > 0)
        {
            AppendAlignment(alignment, width);
        }
        if (escape)
        {
            AppendEscaped(value);
        }
        else
        {
            AppendRaw(value);
        }
        if (alignment < 0)
        {
            AppendAlignment(alignment, width);
        }
    }

    private void AppendAlignment(int alignment, int contentWidth)
    {
        if (alignment == 0)
        {
            return;
        }

        var width = Math.Abs(alignment);
        var pad = width - contentWidth;
        if (pad <= 0)
        {
            return;
        }

        EnsureCapacity(pad);
        for (var i = 0; i < pad; i++)
        {
            _buffer![_length++] = ' ';
        }
    }

    private void EnsureCapacity(int additional)
    {
        if (_buffer is null)
        {
            _buffer = ArrayPool<char>.Shared.Rent(Math.Max(32, additional));
        }

        var required = _length + additional;
        if (required <= _buffer.Length)
        {
            return;
        }

        var newSize = checked(Math.Max(required, _buffer.Length * 2));
        var newBuffer = ArrayPool<char>.Shared.Rent(newSize);
        _buffer.AsSpan(0, _length).CopyTo(newBuffer);
        ArrayPool<char>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }
}
