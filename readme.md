# XenoAtom.Ansi [![ci](https://github.com/XenoAtom/XenoAtom.Ansi/actions/workflows/ci.yml/badge.svg)](https://github.com/XenoAtom/XenoAtom.Ansi/actions/workflows/ci.yml) ![coverage](https://gist.githubusercontent.com/xoofx/4b1dc8d0fa14dd6a3846e78e5f0eafae/raw/dotnet-releaser-coverage-badge-XenoAtom-XenoAtom.Ansi.svg) [![NuGet](https://img.shields.io/nuget/v/XenoAtom.Ansi.svg)](https://www.nuget.org/packages/XenoAtom.Ansi/)

<img align="right" width="256px" height="256px" src="https://raw.githubusercontent.com/XenoAtom/XenoAtom.Ansi/main/img/XenoAtom.Ansi.png">

XenoAtom.Ansi is a fast, allocation-friendly .NET library for building rich ANSI/VT output and processing ANSI text. It helps you emit styled sequences, format markup, tokenize streams, and perform ANSI-aware text operations.

## ✨ Features

- `net10.0`+ library and NativeAOT ready
- Fast, allocation-friendly APIs
- **Rendering / Emitting**
  - `AnsiWriter` fluent API (writes to `TextWriter` or `IBufferWriter<char>`)
  - `AnsiMarkup` for markup strings, including interpolated strings (formatted values are escaped)
  - SGR styling: colors (basic-16, 256-color, truecolor RGB), decorations (bold/dim/italic/underline/etc), reset
  - Capability-aware output (`AnsiCapabilities`) including color downgrading, private-mode gating, and optional safe-mode behavior
  - Cursor/screen helpers (ANSI/DEC/xterm/Windows Terminal): move/position, save/restore, erase (incl. scrollback), insert/delete chars/lines, scrolling + scroll regions, cursor style, mode toggles, tabs, alternate screen, soft reset
  - OSC helpers with configurable terminator (BEL or ST): window title (OSC 0/2), palette edits (OSC 4), hyperlinks (OSC 8)
- **Parsing**
  - Streaming ANSI/VT tokenizer (`AnsiTokenizer`) with chunked parsing support (ESC and 8-bit C1 forms)
  - Token model for Text, selected controls, ESC, CSI, OSC, decoded SGR, and malformed/unknown sequences (tolerant; never throws)
  - Input interpretation helpers for keys, mouse (SGR), and cursor position reports (CPR)
  - Styled runs parser (`AnsiStyledTextParser`) that interprets SGR + OSC 8 into `AnsiStyle`/hyperlink runs
- **Text Utilities**
  - ANSI-aware text helpers (`AnsiText`): strip, visible width measurement (wcwidth), wrap, truncate (optionally preserving ANSI)
- **Color Helpers**
  - Palettes (`AnsiColors`, `AnsiColors.Web`, `AnsiPalettes`) for named colors and xterm-like RGB approximations

> [!NOTE]
> XenoAtom.Ansi is a low-level foundation library, not a terminal UI framework like `Spectre.Console`.
> It focuses on emitting and parsing ANSI/VT sequences against `TextWriter` / character buffers.
> It does not provide widgets, layout, input loops, or terminal state management (those belong in higher-level libraries).

## 🧪 Samples

![Example of XenoAtom.Ansi output](https://raw.githubusercontent.com/XenoAtom/XenoAtom.Ansi/main/doc/XenoAtom.Ansi-screenshot.png)

- `samples/HelloWorld` — basic formatting across a few lines
- `samples/HelloAdvanced` — richer demo (colors, decorations, markup, OSC 8 links, screen helpers)

Run:

- `dotnet run --project samples/HelloWorld/HelloWorld.csproj`
- `dotnet run --project samples/HelloAdvanced/HelloAdvanced.csproj`

## 📖 User Guide

For more details on how to use XenoAtom.Ansi, please visit the [user guide](https://github.com/XenoAtom/XenoAtom.Ansi/blob/main/doc/readme.md).

## 🪪 License

This software is released under the [BSD-2-Clause license](https://opensource.org/licenses/BSD-2-Clause). 

## 🤗 Author

Alexandre Mutel aka [xoofx](https://xoofx.github.io).
