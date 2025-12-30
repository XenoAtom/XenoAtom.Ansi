// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;

namespace XenoAtom.Ansi.Helpers;

internal sealed class PooledCharBuffer : IDisposable
{
    private char[]? _buffer;
    private int _length;

    public PooledCharBuffer(int initialCapacity = 128)
    {
        _buffer = ArrayPool<char>.Shared.Rent(Math.Max(1, initialCapacity));
    }

    public int Length => _length;

    public void Clear() => _length = 0;

    public void Append(char c)
    {
        EnsureNotDisposed();
        EnsureCapacity(1);
        _buffer![_length++] = c;
    }

    public void Append(ReadOnlySpan<char> text)
    {
        EnsureNotDisposed();
        if (text.IsEmpty)
        {
            return;
        }

        EnsureCapacity(text.Length);
        text.CopyTo(_buffer.AsSpan(_length));
        _length += text.Length;
    }

    public ReadOnlySpan<char> AsSpan()
    {
        EnsureNotDisposed();
        return _buffer.AsSpan(0, _length);
    }

    public string ToStringAndClear()
    {
        EnsureNotDisposed();
        var s = _length == 0 ? string.Empty : new string(_buffer!, 0, _length);
        _length = 0;
        return s;
    }

    public override string ToString()
    {
        EnsureNotDisposed();
        return _length == 0 ? string.Empty : new string(_buffer!, 0, _length);
    }

    public void Dispose()
    {
        if (_buffer is not null)
        {
            ArrayPool<char>.Shared.Return(_buffer);
            _buffer = null;
            _length = 0;
        }
    }

    private void EnsureCapacity(int additional)
    {
        if (_buffer!.Length - _length >= additional)
        {
            return;
        }

        var newSize = checked(Math.Max(_length + additional, _buffer.Length * 2));
        var newBuffer = ArrayPool<char>.Shared.Rent(newSize);
        _buffer.AsSpan(0, _length).CopyTo(newBuffer);
        ArrayPool<char>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }

    private void EnsureNotDisposed()
    {
        if (_buffer is null)
        {
            throw new ObjectDisposedException(nameof(PooledCharBuffer));
        }
    }
}

