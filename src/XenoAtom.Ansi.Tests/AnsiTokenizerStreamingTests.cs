// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi.Tokens;

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiTokenizerStreamingTests
{
    [TestMethod]
    public void Tokenize_SplitAcrossChunks_StillParses()
    {
        using var tokenizer = new AnsiTokenizer();

        var tokens1 = tokenizer.Tokenize("a\x1b[".AsSpan(), isFinalChunk: false);
        Assert.HasCount(1, tokens1);
        Assert.AreEqual("a", ((TextToken)tokens1[0]).Text);

        var tokens2 = tokenizer.Tokenize("31mb".AsSpan(), isFinalChunk: true);
        Assert.HasCount(2, tokens2);
        Assert.IsInstanceOfType<SgrToken>(tokens2[0]);
        Assert.AreEqual("b", ((TextToken)tokens2[1]).Text);
    }
}
