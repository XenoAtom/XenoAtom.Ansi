// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using XenoAtom.Ansi;

[MemoryDiagnoser]
public class AnsiMarkupBenchmarks
{
    private string _markup = string.Empty;
    private string _userInput = string.Empty;

    private AnsiBuilder? _builder;
    private AnsiWriter? _writer;
    private AnsiMarkup? _formatter;

    [GlobalSetup]
    public void Setup()
    {
        _markup = "[bold cyan]XenoAtom.Ansi[/] [yellow]markup[/] [underline]parser[/] "
                 + "with [red]colors[/] and [bold]decorations[/], plus rgb(120,200,255) and #11aa44.";

        _userInput = "[red]not-a-tag[/]";

        _builder = new AnsiBuilder(4096);
        _writer = new AnsiWriter(_builder, AnsiCapabilities.Default);
        _formatter = new AnsiMarkup(_writer);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _builder?.Dispose();
        _builder = null;
        _writer = null;
        _formatter = null;
    }

    [Benchmark(Baseline = true)]
    public string Render_ToString()
    {
        _builder!.Clear();
        return AnsiMarkup.Render(_markup.AsSpan(), AnsiCapabilities.Default, initialCapacity: 256);
    }

    [Benchmark]
    public int Write_ToReusableBuilder()
    {
        _builder!.Clear();
        _formatter!.Write(_markup);
        return _builder!.Length;
    }

    [Benchmark]
    public int Write_Interpolated_EscapesValues()
    {
        _builder!.Clear();
        _formatter!.Write($"User input: {_userInput}\n");
        return _builder!.Length;
    }
}

