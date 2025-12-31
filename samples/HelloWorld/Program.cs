using XenoAtom.Ansi;

var writer = new AnsiWriter(Console.Out);
var markup = new AnsiMarkup(writer);

writer.WindowTitle("XenoAtom.Ansi - HelloWorld");

writer.Reset();
markup.Write("[bold]XenoAtom.Ansi[/] - HelloWorld\n");

writer.Write("Plain: Hello, world!\n");

writer
    .Foreground(AnsiColor.Basic16(2))
    .Decorate(AnsiDecorations.Bold)
    .Write("Green bold text\n")
    .Reset();

writer
    .Background(AnsiColor.Basic16(4))
    .Foreground(AnsiColor.Basic16(15))
    .Write("Blue background, white foreground\n")
    .Reset();

markup.Write("Markup: [underline yellow]underlined[/] and [red]colored[/]\n");

writer.Write("\nPress any key to exit...");
Console.ReadKey(intercept: true);
writer.Reset().Write("\n");

