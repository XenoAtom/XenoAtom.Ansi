// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Mouse action as represented by terminal input sequences.
/// </summary>
public enum AnsiMouseAction
{
    /// <summary>
    /// Button press.
    /// </summary>
    Press = 0,

    /// <summary>
    /// Button release.
    /// </summary>
    Release = 1,

    /// <summary>
    /// Mouse move (optionally with a button held depending on protocol).
    /// </summary>
    Move = 2,

    /// <summary>
    /// Wheel scroll.
    /// </summary>
    Wheel = 3,
}
