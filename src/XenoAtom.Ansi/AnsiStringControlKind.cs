// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Identifies an ECMA-48 terminal string control token.
/// </summary>
/// <remarks>
/// Terminal string controls are escape sequences introduced by DCS, SOS, PM, or APC and terminated by ST.
/// </remarks>
public enum AnsiStringControlKind
{
    /// <summary>
    /// Device Control String (<c>ESC P</c> or C1 <c>0x90</c>).
    /// </summary>
    Dcs = 0,

    /// <summary>
    /// Start Of String (<c>ESC X</c> or C1 <c>0x98</c>).
    /// </summary>
    Sos = 1,

    /// <summary>
    /// Privacy Message (<c>ESC ^</c> or C1 <c>0x9E</c>).
    /// </summary>
    Pm = 2,

    /// <summary>
    /// Application Program Command (<c>ESC _</c> or C1 <c>0x9F</c>).
    /// </summary>
    Apc = 3,
}