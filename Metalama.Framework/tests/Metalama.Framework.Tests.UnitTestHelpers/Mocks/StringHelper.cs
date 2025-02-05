// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Text.RegularExpressions;

namespace Metalama.Framework.Tests.UnitTestHelpers.Mocks;

public static class StringHelper
{
    private static readonly Regex _newLineRegex = new( "(\\s*(\r\n|\r|\n)+)", RegexOptions.Compiled | RegexOptions.Multiline );

    public static string NormalizeEndOfLines( this string? s, bool replaceWithSpace = false )
        => string.IsNullOrWhiteSpace( s ) ? "" : _newLineRegex.Replace( s, replaceWithSpace ? " " : Environment.NewLine ).Trim();
}