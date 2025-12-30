// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;

namespace XenoAtom.Ansi;

/// <summary>
/// An allocation-friendly builder for composing ANSI output into a single string.
/// </summary>
/// <remarks>
/// This type implements <see cref="IBufferWriter{T}"/> so it can be used as an output target for <see cref="AnsiWriter"/>.
/// Internally it rents a buffer from <see cref="ArrayPool{T}"/> and returns it on <see cref="Dispose"/>.
/// </remarks>
public sealed class AnsiBuilder : IBufferWriter<char>, IDisposable
{
    private char[]? _buffer;
    private int _length;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnsiBuilder"/> class.
    /// </summary>
    /// <param name="initialCapacity">Initial buffer capacity hint.</param>
    public AnsiBuilder(int initialCapacity = 256)
    {
        if (initialCapacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        }

        _buffer = ArrayPool<char>.Shared.Rent(Math.Max(1, initialCapacity));
    }

    /// <summary>
    /// Gets the number of characters written into the builder.
    /// </summary>
    public int Length => _length;

    /// <summary>
    /// Clears the builder contents without releasing the underlying buffer.
    /// </summary>
    public void Clear() => _length = 0;

    /// <inheritdoc />
    public override string ToString()
    {
        EnsureNotDisposed();
        return _length == 0 ? string.Empty : new string(_buffer!, 0, _length);
    }

    /// <summary>
    /// Appends the specified text.
    /// </summary>
    public void Append(ReadOnlySpan<char> text)
    {
        EnsureNotDisposed();
        if (text.IsEmpty)
        {
            return;
        }

        var span = GetSpan(text.Length);
        text.CopyTo(span);
        Advance(text.Length);
    }

    /// <summary>
    /// Appends the specified text.
    /// </summary>
    public void Append(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Append(text.AsSpan());
    }

    /// <inheritdoc />
    public Memory<char> GetMemory(int sizeHint = 0)
    {
        EnsureNotDisposed();
        if (sizeHint < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeHint));
        }

        EnsureCapacity(sizeHint);
        return _buffer.AsMemory(_length);
    }

    /// <inheritdoc />
    public Span<char> GetSpan(int sizeHint = 0)
    {
        EnsureNotDisposed();
        if (sizeHint < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeHint));
        }

        EnsureCapacity(sizeHint);
        return _buffer.AsSpan(_length);
    }

    /// <inheritdoc />
    public void Advance(int count)
    {
        EnsureNotDisposed();
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var newLength = _length + count;
        if (newLength > _buffer!.Length)
        {
            throw new InvalidOperationException("Advanced past the end of the buffer.");
        }

        _length = newLength;
    }

    /// <summary>
    /// Returns the underlying pooled buffer to the pool.
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

    private void EnsureCapacity(int sizeHint)
    {
        if (sizeHint == 0)
        {
            sizeHint = 1;
        }

        if (_buffer!.Length - _length >= sizeHint)
        {
            return;
        }

        var newSize = checked(Math.Max(_length + sizeHint, _buffer.Length * 2));
        var newBuffer = ArrayPool<char>.Shared.Rent(newSize);
        _buffer.AsSpan(0, _length).CopyTo(newBuffer);
        ArrayPool<char>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }

    private void EnsureNotDisposed()
    {
        if (_buffer is null)
        {
            throw new ObjectDisposedException(nameof(AnsiBuilder));
        }
    }
}
