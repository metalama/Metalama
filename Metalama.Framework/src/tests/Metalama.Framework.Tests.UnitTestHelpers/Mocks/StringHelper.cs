// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Text.RegularExpressions;

namespace Metalama.Framework.Tests.UnitTestHelpers.Mocks;

public static class StringHelper
{
    private static readonly Regex _newLineRegex = new( "(\\s*(\r\n|\r|\n)+)", RegexOptions.Compiled | RegexOptions.Multiline );

    public static string NormalizeEndOfLines( this string? s, bool replaceWithSpace = false )
        => string.IsNullOrWhiteSpace( s ) ? "" : _newLineRegex.Replace( s, replaceWithSpace ? " " : Environment.NewLine ).Trim();
}