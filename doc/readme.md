# XenoAtom.Ansi User Guide

XenoAtom.Ansi is a .NET library for working with ANSI/VT escape sequences:

- Emitting (writing) sequences for styling and a few cursor/screen operations
- Formatting text using lightweight markup into ANSI output
- Parsing sequences from a stream into structured tokens
- ANSI-aware text utilities (strip, measure, wrap, truncate)

It is not a terminal emulator. It does not maintain a full cursor/screen state or emulate scrolling regions.

## Samples

![Example of XenoAtom.Ansi output](https://raw.githubusercontent.com/XenoAtom/XenoAtom.Ansi/main/doc/XenoAtom.Ansi-screenshot.png)

- `samples/HelloWorld` — basic formatting across a few lines
- `samples/HelloAdvanced` — richer demo (colors, decorations, markup, OSC 8 links, screen helpers)

Run from the repo root:

- `dotnet run --project samples/HelloWorld/HelloWorld.csproj`
- `dotnet run --project samples/HelloAdvanced/HelloAdvanced.csproj`

## ANSI/VT terminology (quick reference)

Most "ANSI" terminal features are specified as control sequences introduced by the ESC character:

- **ESC**: the escape control character `U+001B` (`\u001b`)
- **CSI**: Control Sequence Introducer, usually `ESC [`
  - Form: `ESC [` *parameters* *intermediates* *final*
  - Final is in the byte range `0x40..0x7E`
- **SGR**: Select Graphic Rendition, a CSI sequence whose final byte is `m`
  - Examples: `ESC[31m` (red fg), `ESC[1m` (bold), `ESC[0m` (reset)
- **OSC**: Operating System Command, introduced by `ESC ]`
  - Form: `ESC ]` *code* `;` *data* *terminator*
  - Terminator is either **BEL** (`\x07`) or **ST** (`ESC \`)
- **ST**: String Terminator (`ESC \`), used to end OSC/DCS/APC/PM/SOS "string" sequences

## Emitting output with `AnsiWriter`

`AnsiWriter` emits escape sequences to either a `TextWriter` or an `IBufferWriter<char>` and uses a fluent API:

```csharp
using var builder = new AnsiBuilder();
var w = new AnsiWriter(builder);

w.Foreground(AnsiColors.BrightYellow)
 .Decorate(AnsiDecorations.Bold)
 .Write("Warning: ")
 .ResetStyle()
 .Write("disk is almost full");

var s = builder.ToString();
```

### Capabilities and feature gating

Not all environments support ANSI or the same color depth. Use `AnsiCapabilities` to gate output:

```csharp
var caps = AnsiCapabilities.Default with
{
    AnsiEnabled = true,
    ColorLevel = AnsiColorLevel.Colors256,
    SupportsOsc8 = false,
};

using var builder = new AnsiBuilder();
var w = new AnsiWriter(builder, caps);
```

If `AnsiEnabled` is `false`, style/cursor/hyperlink methods become no-ops (text still writes).

### Colors and decorations

Foreground/background:

```csharp
w.Foreground(AnsiColors.Red).Write("red");
w.Background(AnsiColors.Blue).Write("on blue");
w.ResetStyle();
```

256-color indexed:

```csharp
w.Foreground(AnsiColor.Indexed256(208)).Write("orange-ish");
```

Truecolor (24-bit RGB):

```csharp
w.Foreground(AnsiColor.Rgb(255, 128, 0)).Write("rgb");
```

Decorations:

```csharp
w.Decorate(AnsiDecorations.Underline | AnsiDecorations.Bold).Write("emphasis");
w.Undecorate(AnsiDecorations.Underline).Write("still bold");
w.ResetStyle();
```

### Styling via `AnsiStyle`

You can author a reusable style and apply it:

```csharp
var title = new AnsiStyle
{
    Foreground = AnsiColors.Cyan,
    Decorations = AnsiDecorations.Bold,
};

w.Style(title).Write("Title").ResetStyle();
```

### Minimal style transitions

For live/progress output, you typically track your current style and emit only the delta:

```csharp
var current = AnsiStyle.Default;
var next = current with { Foreground = AnsiColors.Green };

w.StyleTransition(current, next).Write("OK");
current = next;
```

This minimizes `ESC[0m` usage unless `AnsiCapabilities.SafeMode` is enabled.

### Cursor and erase helpers

These helpers write common CSI sequences:

```csharp
w.Up(1).EraseLine(2);          // move up, clear entire line
w.MoveTo(1, 1);               // 1-based cursor position
w.CursorVisible(false);       // ESC[?25l
```

Alternate screen (for full-screen UIs):

```csharp
w.AlternateScreen(true);
// ...
w.AlternateScreen(false);
```

### OSC 8 hyperlinks

If `SupportsOsc8` is enabled, you can emit hyperlinks:

```csharp
w.BeginLink("https://example.com").Write("click here").EndLink();
```

## Markup with `AnsiMarkup`

If you prefer authoring styled output as a single string, `AnsiMarkup` can parse a simple markup syntax and emit the corresponding ANSI sequences (using `AnsiWriter` under the hood).

```csharp
var s = AnsiMarkup.Render("[bold yellow on blue]Warning[/] disk is almost full");
```

Interpolated values are escaped automatically (so user input cannot inject markup tags):

```csharp
var userInput = "[red]not actually red[/]";
var s = AnsiMarkup.Render($"[red]{userInput}[/]");
```

### Tag syntax

- Tags are written as `[ ... ]` and can be nested.
- Close the most recent tag with `[/]`.
- Escape literal brackets with `[[` and `]]` (e.g. `AnsiMarkup.Render("a[[b]]")` renders `a[b]`).

### Supported styles

- Decorations: `bold`, `dim`, `italic`, `underline`, `blink`, `invert`, `hidden`, `strikethrough`
- Foreground colors: `black`, `red`, `green`, `yellow`, `blue`, `magenta`, `cyan`, `white`, `gray`/`grey`, and `bright*` variants (e.g. `brightred`, `bright-red`)
- Background colors: `on <color>`, `bg:<color>`, or `bg=<color>`
- 256-color indexed: `0..255` (foreground), `bg:0..255` (background)
- Truecolor: `#RRGGBB` or `rgb(r,g,b)` (foreground), and `bg:#RRGGBB` (background)

## Parsing input with `AnsiTokenizer`

`AnsiTokenizer` parses a stream into tokens:

- `TextToken`: plain text
- `ControlToken`: selected C0 controls (CR/LF/TAB/BEL)
- `EscToken`: non-CSI escape sequences (e.g. `ESC 7`, `ESC 8`, `ESC \`)
- `CsiToken`: syntactic CSI tokens (parameters + intermediates + final)
- `SgrToken`: decoded SGR operations (a decoded `CSI ... m`)
- `OscToken`: parsed OSC code + data
- `UnknownEscapeToken`: malformed/unsupported sequences (never throws)

Example:

```csharp
using var tok = new AnsiTokenizer();
var tokens = tok.Tokenize("a\u001b[31mb".AsSpan(), isFinalChunk: true);
```

### Streaming / chunked parsing

If input arrives in chunks, keep `isFinalChunk: false` until the end:

```csharp
using var tok = new AnsiTokenizer();
var tokens = new List<AnsiToken>();
tok.Tokenize("a\u001b[".AsSpan(), isFinalChunk: false, tokens);
tok.Tokenize("31mb".AsSpan(), isFinalChunk: true, tokens);
```

## Styled runs with `AnsiStyledTextParser`

`AnsiStyledTextParser` interprets SGR and OSC 8 into styled runs suitable for rendering:

```csharp
using var parser = new AnsiStyledTextParser();
var runs = parser.Parse("ab\u001b[31mcd\u001b[0mef".AsSpan());
// runs: ("ab", default), ("cd", red), ("ef", default)
```

## ANSI-aware text utilities (`AnsiText`)

These utilities ignore escape sequences and operate on visible text widths.
Unicode width is computed via the `Wcwidth` NuGet package.

```csharp
var plain = AnsiText.Strip("a\u001b[31mb".AsSpan());          // "ab"
var width = AnsiText.GetVisibleWidth("a界b".AsSpan());       // 4 (界 is width 2)
var wrapped = AnsiText.Wrap("abcd", width: 2, preserveAnsi: false);
var truncated = AnsiText.Truncate("hello world", width: 5);  // "hell…"
```

## Palettes

- `AnsiColors` provides named basic-16 palette indices (e.g. `AnsiColors.Red`).
- `AnsiPalettes` provides xterm-like RGB approximations for palette indices when rendering outside a terminal.

```csharp
var rgb = AnsiPalettes.GetBasic16Rgb(AnsiColors.Red.Index); // (R,G,B) approximation
```

## Appendix: Supported ANSI/VT sequences

This section describes what XenoAtom.Ansi explicitly supports for **writing** (emitting) and **reading** (tokenizing/decoding).

### Writing (emitting) support (`AnsiWriter`)

`AnsiWriter` emits 7-bit ANSI/VT sequences (it always uses `ESC`-prefixed sequences, not 8-bit C1 control codes).

#### SGR (Select Graphic Rendition) — CSI `... m`

| API | Emits | Notes |
|---|---|---|
| `ResetStyle()` / `Reset()` | `ESC[0m` | Resets all attributes |
| `Foreground(AnsiColor.Default)` | `ESC[39m` | Default foreground |
| `Foreground(AnsiColor.Basic16(0..15))` | `ESC[30..37m` / `ESC[90..97m` | 16-color palette |
| `Foreground(AnsiColor.Indexed256(0..255))` | `ESC[38;5;<n>m` | 256-color indexed |
| `Foreground(AnsiColor.Rgb(r,g,b))` | `ESC[38;2;<r>;<g>;<b>m` | May downgrade based on `AnsiCapabilities.ColorLevel` |
| `Background(AnsiColor.Default)` | `ESC[49m` | Default background |
| `Background(AnsiColor.Basic16(0..15))` | `ESC[40..47m` / `ESC[100..107m` | 16-color palette |
| `Background(AnsiColor.Indexed256(0..255))` | `ESC[48;5;<n>m` | 256-color indexed |
| `Background(AnsiColor.Rgb(r,g,b))` | `ESC[48;2;<r>;<g>;<b>m` | May downgrade based on `AnsiCapabilities.ColorLevel` |
| `Style(AnsiStyle)` | SGR sequence(s) | Applies a style starting from the default style |
| `StyleTransition(from,to,...)` | SGR sequence(s) | Minimal delta; more aggressive output when `AnsiCapabilities.SafeMode` is `true` |

Decoration parameters used by `Decorate(...)` / `Undecorate(...)`:

| Decoration | Enable SGR | Disable SGR |
|---|---:|---:|
| Bold | `1` | `22` |
| Dim | `2` | `22` |
| Italic | `3` | `23` |
| Underline | `4` | `24` |
| Blink | `5` | `25` |
| Invert | `7` | `27` |
| Hidden | `8` | `28` |
| Strikethrough | `9` | `29` |

#### Cursor / erase (CSI)

| API | Emits | Notes |
|---|---|---|
| `Up(n)` / `CursorUp(n)` | `ESC[<n>A` | Cursor up |
| `Down(n)` / `CursorDown(n)` | `ESC[<n>B` | Cursor down |
| `Forward(n)` / `CursorForward(n)` | `ESC[<n>C` | Cursor forward |
| `Back(n)` / `CursorBack(n)` | `ESC[<n>D` | Cursor back |
| `NextLine(n)` | `ESC[<n>E` | Cursor next line |
| `PreviousLine(n)` | `ESC[<n>F` | Cursor previous line |
| `CursorHorizontalAbsolute(col)` | `ESC[<col>G` | 1-based |
| `CursorVerticalAbsolute(row)` | `ESC[<row>d` | 1-based |
| `MoveTo(row,col)` / `CursorPosition(row,col)` | `ESC[<row>;<col>H` | 1-based |
| `HorizontalAndVerticalPosition(row,col)` | `ESC[<row>;<col>f` | 1-based |
| `EraseLine(mode)` / `EraseInLine(mode)` | `ESC[<mode>K` | `mode=0` is emitted as `ESC[K` |
| `EraseDisplay(mode)` / `EraseInDisplay(mode)` | `ESC[<mode>J` | `mode=0` is emitted as `ESC[J` |
| `EraseScrollback()` | `ESC[3J` | Clear scrollback (xterm/Windows Terminal) |
| `EraseCharacters(n)` | `ESC[<n>X` | Erase characters (ECH) |
| `InsertCharacters(n)` | `ESC[<n>@` | Insert characters (ICH) |
| `DeleteCharacters(n)` | `ESC[<n>P` | Delete characters (DCH) |
| `InsertLines(n)` | `ESC[<n>L` | Insert lines (IL) |
| `DeleteLines(n)` | `ESC[<n>M` | Delete lines (DL) |
| `ScrollUp(n)` | `ESC[<n>S` | Scroll up (SU) |
| `ScrollDown(n)` | `ESC[<n>T` | Scroll down (SD) |
| `SetScrollRegion(top,bottom)` | `ESC[<top>;<bottom>r` | DECSTBM |
| `ResetScrollRegion()` | `ESC[r` | Reset DECSTBM |

#### Simple ESC sequences

| API | Emits | Notes |
|---|---|---|
| `ReverseIndex()` | `ESC M` | RI |
| `KeypadApplicationMode()` | `ESC=` | DECKPAM |
| `KeypadNumericMode()` | `ESC>` | DECKPNM |
| `EnterLineDrawingMode()` | `ESC(0` | DEC Special Graphics / line drawing (G0) |
| `ExitLineDrawingMode()` | `ESC(B` | US-ASCII (G0) |

#### Cursor save/restore (ESC)

| API | Emits | Notes |
|---|---|---|
| `SaveCursor()` | `ESC7` | DECSC |
| `RestoreCursor()` | `ESC8` | DECRC |
| `SaveCursorPosition()` | `ESC[s` | SCOSC |
| `RestoreCursorPosition()` | `ESC[u` | SCORC |

#### Tabs

| API | Emits | Notes |
|---|---|---|
| `HorizontalTabSet()` | `ESC H` | HTS |
| `CursorForwardTab(n)` | `ESC[<n>I` | CHT |
| `CursorBackTab(n)` | `ESC[<n>Z` | CBT |
| `ClearTabStop()` | `ESC[0g` | TBC (current column) |
| `ClearAllTabStops()` | `ESC[3g` | TBC (all columns) |

#### DEC private modes (CSI with `?`)

| API | Emits | Notes |
|---|---|---|
| `CursorVisible(true)` / `ShowCursor(true)` | `ESC[?25h` | Show cursor |
| `CursorVisible(false)` / `ShowCursor(false)` | `ESC[?25l` | Hide cursor |
| `AlternateScreen(true)` / `EnterAlternateScreen()` | `ESC[?1049h` | Enter alternate screen |
| `AlternateScreen(false)` / `LeaveAlternateScreen()` | `ESC[?1049l` | Leave alternate screen |
| `CursorKeysApplicationMode(true/false)` | `ESC[?1h` / `ESC[?1l` | DECCKM |
| `CursorBlinking(true/false)` | `ESC[?12h` / `ESC[?12l` | ATT160 cursor blinking |
| `Columns132(true/false)` | `ESC[?3h` / `ESC[?3l` | DECCOLM (80/132 columns) |
| `PrivateMode(n, true/false)` | `ESC[?<n>h` / `ESC[?<n>l` | Generic DEC private mode (DECSET/DECRST) |

#### Soft reset (CSI with intermediate `!`)

| API | Emits | Notes |
|---|---|---|
| `SoftReset()` | `ESC[!p` | “Soft reset” |

#### Cursor style (CSI with intermediate `SP`)

| API | Emits | Notes |
|---|---|---|
| `CursorStyle(AnsiCursorStyle)` | `ESC[<n> q` | DECSCUSR (0..6) |

#### Queries (CSI)

| API | Emits | Notes |
|---|---|---|
| `RequestCursorPosition()` | `ESC[6n` | DECXCPR; reply typically `ESC[<r>;<c>R` |
| `RequestDeviceAttributes()` | `ESC[c` | DA; reply typically `ESC[?...c` |

#### OSC (Operating System Command)

| API | Emits | Notes |
|---|---|---|
| `WindowTitle(title)` | `ESC]2;<title>` + terminator | OSC 2 window title |
| `IconAndWindowTitle(title)` | `ESC]0;<title>` + terminator | OSC 0 icon + window title |
| `SetPaletteColor(i,r,g,b)` | `ESC]4;<i>;rgb:<rr>/<gg>/<bb>` + terminator | OSC 4 palette entry |
| `BeginLink(uri, id)` | `ESC]8;id=<id>;<uri>` + terminator | OSC 8 hyperlink (id is optional) |
| `EndLink()` | `ESC]8;;` + terminator | Ends OSC 8 hyperlink |

| `AnsiCapabilities.OscTermination` | Terminator emitted |
|---|---|
| `AnsiOscTermination.StringTerminator` | `ESC\` (ST) |
| `AnsiOscTermination.Bell` | `BEL` (`\x07`) |

### Reading support (`AnsiTokenizer`)

The tokenizer is designed to be streaming and tolerant: it never throws on malformed sequences and will surface them as `UnknownEscapeToken`.

#### Token kinds

| Input | Token | Notes |
|---|---|---|
| Plain text | `TextToken` | Fast path when no `ESC` present |
| CR/LF/TAB/BEL | `ControlToken` | Only this subset is surfaced as control tokens |
| `ESC` sequences (non-CSI) | `EscToken` | Captures intermediates (`0x20..0x2F`) + final (`0x30..0x7E`) |
| `ESC O` ... | `Ss3Token` | SS3 (common for input keys: application cursor keys, F1–F4) |
| `ESC [` ... | `CsiToken` or `SgrToken` | `SgrToken` only when final is `m` and `DecodeSgr` is enabled |
| `ESC ]` ... | `OscToken` | OSC code + data |
| `CSI` (`0x9B`) ... | `CsiToken` or `SgrToken` | Also supports 8-bit C1 CSI |
| `OSC` (`0x9D`) ... | `OscToken` | Also supports 8-bit C1 OSC |
| Malformed/unsupported/over-limit | `UnknownEscapeToken` | Best-effort recovery; never throws |

#### ESC handling

| Sequence | Tokenization | Notes |
|---|---|---|
| `ESC [` | Starts CSI | CSI grammar: parameters (`0x30..0x3F`) + intermediates (`0x20..0x2F`) + final (`0x40..0x7E`) |
| `ESC ]` | Starts OSC | OSC terminates with `BEL` (`\x07`) or `ST` (`ESC\`) |
| `ESC O` | `Ss3Token` | SS3 used for input keys (e.g. `ESC O A` for Up in application mode) |
| `ESC P` / `ESC X` / `ESC ^` / `ESC _` | Skipped until `ST` | DCS/SOS/PM/APC “string” functions are not decoded; emitted as `UnknownEscapeToken` |
| Other `ESC` sequences | `EscToken` | Examples: `ESC7`, `ESC8`, `ESC\` |

#### CSI token capture (`CsiToken`)

| Field | Meaning |
|---|---|
| `Parameters` | Numeric params parsed from digits separated by `;` (and `:` is accepted and treated like `;`) |
| `Intermediates` | Intermediate bytes (`0x20..0x2F`) captured as a string |
| `Final` | Final byte (`0x40..0x7E`) identifying the command |
| `PrivateMarker` | One of `<`, `=`, `>`, `?` when present as the first parameter byte (common for DEC/xterm private modes) |
| `Raw` | Raw sequence text when available |

#### SGR decoding (`SgrToken`)

When `AnsiTokenizerOptions.DecodeSgr` is enabled, `CSI ... m` is decoded into a list of `AnsiSgrOp` operations.
Unknown or malformed SGR parameters are ignored (best-effort decoding).

| SGR parameter(s) | Meaning | Operation(s) |
|---|---|---|
| `0` | Reset all attributes | `Reset` |
| `1,2,3,4,5,7,8,9` | Enable decorations | `SetDecoration(..., enabled: true)` |
| `22,23,24,25,27,28,29` | Disable decorations | `SetDecoration(..., enabled: false)` (note: `22` clears bold+dim) |
| `30..37`, `90..97` | Foreground basic-16 | `SetForeground(Basic16(...))` |
| `40..47`, `100..107` | Background basic-16 | `SetBackground(Basic16(...))` |

### Input parsing helpers

While `AnsiTokenizer` is primarily syntactic, it can also be used to decode common terminal input sequences via convenience helpers:

- `CsiToken.TryGetCursorPositionReport(out AnsiCursorPosition)` for CPR replies (`ESC[<row>;<col>R`)
- `CsiToken.TryGetSgrMouseEvent(out AnsiMouseEvent)` for SGR mouse (`ESC[<b;x;yM/m`)
- `AnsiToken.TryGetKeyEvent(out AnsiKeyEvent)` for common key sequences (arrows, Home/End, Insert/Delete, F1–F12, etc.)

For test and host scenarios, `AnsiWriter` also provides helper instance methods to emit these input sequences.
| `39` / `49` | Reset fg/bg to default | `SetForeground(Default)` / `SetBackground(Default)` |
| `38;5;<n>` / `48;5;<n>` | 256-color indexed fg/bg | `SetForeground(Indexed256(n))` / `SetBackground(Indexed256(n))` |
| `38;2;<r>;<g>;<b>` / `48;2;<r>;<g>;<b>` | Truecolor fg/bg | `SetForeground(Rgb(...))` / `SetBackground(Rgb(...))` |

#### OSC parsing (`OscToken`)

| Input form | Terminator | Parsed fields |
|---|---|---|
| `ESC ] <code> ; <data> ...` | `BEL` (`\x07`) or `ST` (`ESC\`) | `Code` is digits before the first `;`, `Data` is everything after |

### Reading support (`AnsiStyledTextParser`)

`AnsiStyledTextParser` provides a higher-level “runs” view intended for rendering:

- Applies `SgrToken` operations to produce `AnsiStyle` changes per run
- Interprets OSC 8 (`OscToken` with `Code == 8`) to track an active hyperlink per run
- Does not interpret cursor movement/erasure; runs are produced in source order
