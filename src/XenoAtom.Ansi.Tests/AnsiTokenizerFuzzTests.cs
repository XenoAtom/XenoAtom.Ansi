// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class AnsiTokenizerFuzzTests
{
    [TestMethod]
    public void Tokenize_RandomInput_DoesNotThrow()
    {
        using var tokenizer = new AnsiTokenizer();
        var rng = new Random(1234);

        for (var i = 0; i < 200; i++)
        {
            var s = BuildRandom(rng, length: 256);
            _ = tokenizer.Tokenize(s.AsSpan(), isFinalChunk: true);
        }
    }

    private static string BuildRandom(Random rng, int length)
    {
        var chars = new char[length];
        for (var i = 0; i < chars.Length; i++)
        {
            var roll = rng.Next(100);
            chars[i] = roll switch
            {
                < 3 => '\x1b',
                < 6 => '[',
                < 9 => ']',
                < 12 => ';',
                < 15 => 'm',
                < 18 => '\\',
                < 20 => '\x07',
                _ => (char)rng.Next(32, 127),
            };
        }
        return new string(chars);
    }
}

