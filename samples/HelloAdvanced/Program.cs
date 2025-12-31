using XenoAtom.Ansi;

var writer = new AnsiWriter(Console.Out);
var markup = new AnsiMarkup(writer);

writer.WindowTitle("XenoAtom.Ansi - HelloAdvanced");

writer.EnterAlternateScreen();
try
{
    writer.EraseDisplay(2).MoveTo(1, 1);

    markup.Write("[bold cyan]XenoAtom.Ansi[/] - [bold]HelloAdvanced[/]\n");
    writer.Write("This sample emits ANSI/VT sequences; run it in a terminal that supports ANSI.\n\n");

    writer.CursorStyle(AnsiCursorStyle.SteadyBar);
    writer.CursorVisible(true);

    markup.Write("[bold]Colors[/]\n");
    writer
        .Foreground(AnsiColor.Basic16(1)).Write("  basic-16: red\n").Reset()
        .Foreground(AnsiColor.Indexed256(208)).Write("  256-color: orange-ish (index 208)\n").Reset()
        .Foreground(AnsiColor.Rgb(120, 200, 255)).Write("  truecolor: rgb(120,200,255)\n").Reset()
        .Write("\n")
        .Write("  Spectrum (truecolor):\n");
    const int spectrumCols = 24;
    const int spectrumRows = 8;
    for (var y = 0; y < spectrumRows; y++)
    {
        writer.Write("  ");
        var v = 1.0 - (y * (0.6 / (spectrumRows - 1))); // 1.0 .. 0.4
        for (var x = 0; x < spectrumCols; x++)
        {
            var h = (x * 360.0) / spectrumCols;
            const double s = 1.0;

            var c = v * s;
            var hp = h / 60.0;
            var x1 = c * (1.0 - Math.Abs((hp % 2.0) - 1.0));
            var m = v - c;

            double r1, g1, b1;
            if (hp < 1.0) (r1, g1, b1) = (c, x1, 0.0);
            else if (hp < 2.0) (r1, g1, b1) = (x1, c, 0.0);
            else if (hp < 3.0) (r1, g1, b1) = (0.0, c, x1);
            else if (hp < 4.0) (r1, g1, b1) = (0.0, x1, c);
            else if (hp < 5.0) (r1, g1, b1) = (x1, 0.0, c);
            else (r1, g1, b1) = (c, 0.0, x1);

            var r = (byte)Math.Clamp((int)Math.Round((r1 + m) * 255.0), 0, 255);
            var g = (byte)Math.Clamp((int)Math.Round((g1 + m) * 255.0), 0, 255);
            var b = (byte)Math.Clamp((int)Math.Round((b1 + m) * 255.0), 0, 255);

            writer.Background(AnsiColor.Rgb(r, g, b)).Write("  ");
        }
        writer.Reset().Write("\n");
    }

    writer.Write("  Grayscale:\n");
    writer.Write("  ");
    for (var x = 0; x < spectrumCols; x++)
    {
        var gray = (byte)Math.Clamp((int)Math.Round((x * 255.0) / (spectrumCols - 1)), 0, 255);
        writer.Background(AnsiColor.Rgb(gray, gray, gray)).Write("  ");
    }
    writer.Reset().Write("\n");

    markup.Write("\n[bold]Decorations (SGR)[/]\n");
    writer
        .Write("  ")
        .Decorate(AnsiDecorations.Bold).Write("bold").Reset()
        .Write(" / ")
        .Decorate(AnsiDecorations.Dim).Write("dim").Reset()
        .Write(" / ")
        .Decorate(AnsiDecorations.Italic).Write("italic").Reset()
        .Write(" / ")
        .Decorate(AnsiDecorations.Underline).Write("underline").Reset()
        .Write(" / ")
        .Decorate(AnsiDecorations.Strikethrough).Write("strikethrough").Reset()
        .Write("\n");

    writer.Write("  ");
    writer.Foreground(AnsiColor.Basic16(15)).Background(AnsiColor.Basic16(4));
    writer.Decorate(AnsiDecorations.Invert).Write("invert").Reset();
    writer.Write(" (reverse video)\n");

    writer.Write("  ");
    writer.Decorate(AnsiDecorations.Blink).Write("blink").Reset();
    writer.Write(" (often not supported)\n");

    writer.Write("  ");
    writer.Background(AnsiColor.Basic16(7)).Foreground(AnsiColor.Basic16(0));
    writer.Decorate(AnsiDecorations.Hidden).Write("hidden").Reset();
    writer.Write(" (conceal; may render as blank)\n");

    writer.Write("  ");
    writer
        .Foreground(AnsiColor.Indexed256(45))
        .Decorate(AnsiDecorations.Bold | AnsiDecorations.Underline)
        .Write("combined: bold + underline")
        .Reset()
        .Write("\n");

    var userInput = "[red]not-a-tag[/]";
    markup
        .Write("\n[bold]Markup (interpolated values are escaped)[/]\n")
        .Write($"  User input: {userInput}\n")
        .Write("  Styled: [bold yellow on blue]Hello markup[/]\n");

    markup.Write("\n[bold]Hyperlinks (OSC 8)[/]\n");
    writer.Write("  ").BeginLink("https://github.com/XenoAtom/XenoAtom.Ansi").Write("XenoAtom.Ansi on GitHub").EndLink().Write("\n");

    markup.Write("\n[bold]Cursor / screen helpers[/]\n");
    writer.Write("  Drawing a small box with DEC line drawing:\n");
    const int innerWidth = 8;
    const string content = " box ";
    var padding = Math.Max(0, innerWidth - content.Length);

    writer
        .Write("  ").EnterLineDrawingMode().Write("lqqqqqqqqk").ExitLineDrawingMode().Write("\n")
        .Write("  ").EnterLineDrawingMode().Write("x").ExitLineDrawingMode()
        .Write(content)
        .Write(new string(' ', padding))
        .EnterLineDrawingMode().Write("x").ExitLineDrawingMode()
        .Write("\n")
        .Write("  ").EnterLineDrawingMode().Write("mqqqqqqqqj").ExitLineDrawingMode().Write("\n");

    writer.Write("\nPress any key to exit...");
    Console.ReadKey(intercept: true);
}
finally
{
    writer.Reset().CursorStyle(AnsiCursorStyle.Default).LeaveAlternateScreen();
}
