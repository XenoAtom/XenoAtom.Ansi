// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Ansi;

/// <summary>
/// Represents a syntactically parsed Kitty graphics protocol reply.
/// </summary>
/// <param name="ImageId">The optional <c>i=</c> image id parameter.</param>
/// <param name="PlacementId">The optional <c>p=</c> placement id parameter.</param>
/// <param name="Status">The reply status, for example <c>OK</c> or an error code.</param>
/// <param name="Parameters">The raw comma-separated reply parameters before the status separator.</param>
/// <param name="Message">Optional text after a <c>:</c> in the reply status payload.</param>
public readonly record struct AnsiKittyGraphicsReply(int? ImageId, int? PlacementId, string Status, string Parameters, string? Message);