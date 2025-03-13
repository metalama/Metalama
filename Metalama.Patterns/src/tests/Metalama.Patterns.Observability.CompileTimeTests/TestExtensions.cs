// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using Microsoft.CodeAnalysis;
using Xunit.Abstractions;

namespace Metalama.Patterns.Observability.CompileTimeTests;

internal static class TestExtensions
{
    // ReSharper disable once UnusedMember.Global
    public static void Add( this List<string> strings, IDiagnostic diagnostic, Location? location )
    {
        var start = location?.GetLineSpan().StartLinePosition ?? default;
        var end = location?.GetLineSpan().EndLinePosition ?? default;

        strings.Add( $"{diagnostic}@({start.Line},{start.Character})-({end.Line},{end.Character})" );
    }

    public static void WriteLines( this ITestOutputHelper testOutput, IEnumerable<string> strings )
    {
        foreach ( var s in strings )
        {
            testOutput.WriteLine( s );
        }
    }
}