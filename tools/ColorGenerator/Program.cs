using System.Drawing;
using System.Text;

static DirectoryInfo FindRepoRoot()
{
    var start = new DirectoryInfo(AppContext.BaseDirectory);
    for (var dir = start; dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "src", "XenoAtom.Ansi", "XenoAtom.Ansi.csproj")))
        {
            return dir;
        }
    }

    throw new InvalidOperationException("Unable to locate repo root.");
}

static string ToLookupKey(string name) => name.ToLowerInvariant();

static string ToHex(byte r, byte g, byte b) => $"{r:X2}{g:X2}{b:X2}";

var repoRoot = FindRepoRoot();
var outputPath = Path.Combine(repoRoot.FullName, "src", "XenoAtom.Ansi", "AnsiColors.Web.g.cs");

var colors = Enum.GetValues<KnownColor>()
    .Select(Color.FromKnownColor)
    .Where(c => !c.IsSystemColor && !c.IsEmpty && c.A == 255 && !string.Equals(c.Name, "Transparent", StringComparison.OrdinalIgnoreCase))
    .Select(c => (Name: c.Name, c.R, c.G, c.B))
    .OrderBy(c => c.Name, StringComparer.Ordinal)
    .ToList();

var sb = new StringBuilder();
sb.AppendLine("// Copyright (c) Alexandre Mutel. All rights reserved.");
sb.AppendLine("// Licensed under the BSD-Clause 2 license.");
sb.AppendLine("// See license.txt file in the project root for full license information.");
sb.AppendLine();
sb.AppendLine("namespace XenoAtom.Ansi;");
sb.AppendLine();
sb.AppendLine("public static partial class AnsiColors");
sb.AppendLine("{");
sb.AppendLine("    /// <summary>");
sb.AppendLine("    /// Web (CSS/SVG/X11) named colors as truecolor RGB values.");
sb.AppendLine("    /// </summary>");
sb.AppendLine("    public static class Web");
sb.AppendLine("    {");

foreach (var c in colors)
{
    var hex = ToHex(c.R, c.G, c.B);
    sb.AppendLine($"        /// <summary>Web (CSS/SVG/X11) named color <c>{c.Name}</c> (#{hex}).</summary>");
    sb.AppendLine($"        public static AnsiColor {c.Name} => AnsiColor.Rgb({c.R}, {c.G}, {c.B});");
    sb.AppendLine();
}

sb.AppendLine("    }");
sb.AppendLine();
sb.AppendLine("    private static partial void AddWebColors(Dictionary<string, AnsiColor> colors)");
sb.AppendLine("    {");
foreach (var c in colors)
{
    var key = ToLookupKey(c.Name);
    sb.AppendLine($"        colors[\"{key}\"] = AnsiColor.Rgb({c.R}, {c.G}, {c.B});");
}
sb.AppendLine("    }");
sb.AppendLine("}");

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

Console.WriteLine($"Generated {colors.Count} web colors -> {outputPath}");
