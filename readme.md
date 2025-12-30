# XenoAtom.Ansi [![ci](https://github.com/XenoAtom/XenoAtom.Ansi/actions/workflows/ci.yml/badge.svg)](https://github.com/XenoAtom/XenoAtom.Ansi/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/nuget/v/XenoAtom.Ansi.svg)](https://www.nuget.org/packages/XenoAtom.Ansi/)

<img align="right" width="160px" height="160px" src="https://raw.githubusercontent.com/XenoAtom/XenoAtom.Ansi/main/img/XenoAtom.Ansi.png">

XenoAtom.Ansi is a small .NET library for working with ANSI/VT escape sequences.

## ✨ Features

- Emit ANSI/VT sequences (SGR colors/styles, cursor/erase ops, OSC 8 hyperlinks)
- Streaming tokenizer for ANSI/VT input (Text / Control / CSI / OSC / SGR / Unknown)
- ANSI-aware text utilities (strip, measure, wrap, truncate)
- Basic palettes (`AnsiColors`, `AnsiPalettes`) for named colors and RGB approximations
- Optional Windows helper to enable Virtual Terminal Processing

## 📖 User Guide

For more details on how to use XenoAtom.Ansi, please visit the [user guide](https://github.com/XenoAtom/XenoAtom.Ansi/blob/main/doc/readme.md).

## 🪪 License

This software is released under the [BSD-2-Clause license](https://opensource.org/licenses/BSD-2-Clause). 

## 🤗 Author

Alexandre Mutel aka [xoofx](https://xoofx.github.io).
