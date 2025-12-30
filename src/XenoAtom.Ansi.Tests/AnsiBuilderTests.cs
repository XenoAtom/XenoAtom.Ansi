// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiBuilderTests
{
    [TestMethod]
    public void GetMemory_And_Advance_WritesToBuffer()
    {
        using var builder = new AnsiBuilder();
        var mem = builder.GetMemory(3);
        mem.Span[0] = 'a';
        mem.Span[1] = 'b';
        mem.Span[2] = 'c';
        builder.Advance(3);

        Assert.AreEqual(3, builder.Length);
        Assert.AreEqual("abc", builder.ToString());
    }

    [TestMethod]
    public void UnsafeAsSpan_ReturnsWrittenContents()
    {
        using var builder = new AnsiBuilder();
        builder.Append("hello");

        var span = builder.UnsafeAsSpan();
        Assert.AreEqual(5, span.Length);
        Assert.AreEqual('h', span[0]);
        Assert.AreEqual('o', span[^1]);
    }

    [TestMethod]
    public void Clear_ResetsLength()
    {
        using var builder = new AnsiBuilder();
        builder.Append("x");
        builder.Clear();

        Assert.AreEqual(0, builder.Length);
        Assert.AreEqual(string.Empty, builder.ToString());
    }
}

