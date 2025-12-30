# XenoAtom.Ansi [![ci](https://github.com/XenoAtom/XenoAtom.Ansi/actions/workflows/ci.yml/badge.svg)](https://github.com/XenoAtom/XenoAtom.Ansi/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/nuget/v/XenoAtom.Ansi.svg)](https://www.nuget.org/packages/XenoAtom.Ansi/)

<img align="right" width="160px" height="160px" src="https://raw.githubusercontent.com/XenoAtom/XenoAtom.Ansi/main/img/XenoAtom.Ansi.png">

XenoAtom.Ansi is a small .NET library for working with ANSI/VT escape sequences.

## ✨ Features

- Emit ANSI/VT sequences with `AnsiWriter` (writes to `TextWriter` or `IBufferWriter<char>`)
- SGR styling: reset, decorations (bold/dim/italic/underline/etc), basic-16, 256-color, and truecolor RGB (with capability-based downgrading)
- Minimal style transitions (`WriteStyleTransition`) for live/progress output, with optional “safe mode” behavior via `AnsiCapabilities`
- Cursor/screen helpers: move/position, save/restore cursor, erase line/display, show/hide cursor, alternate screen, soft reset
- OSC support for hyperlinks (OSC 8), with configurable terminator (BEL or ST)
- Streaming ANSI/VT tokenizer (`AnsiTokenizer`) with chunked parsing support
- Token model for Text, selected controls, ESC, CSI, OSC, decoded SGR, and malformed/unknown sequences (tolerant; never throws)
- Styled runs parser (`AnsiStyledTextParser`) that interprets SGR + OSC 8 into `AnsiStyle`/hyperlink runs
- ANSI-aware text utilities (`AnsiText`): strip, visible width measurement (wcwidth), wrap, truncate (optionally preserving ANSI)
- Palettes (`AnsiColors`, `AnsiPalettes`) for named colors and xterm-like RGB approximations
- Optional Windows helper to enable Virtual Terminal Processing

## 📖 User Guide

For more details on how to use XenoAtom.Ansi, please visit the [user guide](https://github.com/XenoAtom/XenoAtom.Ansi/blob/main/doc/readme.md).

## 🪪 License

This software is released under the [BSD-2-Clause license](https://opensource.org/licenses/BSD-2-Clause). 

## 🤗 Author

Alexandre Mutel aka [xoofx](https://xoofx.github.io).
