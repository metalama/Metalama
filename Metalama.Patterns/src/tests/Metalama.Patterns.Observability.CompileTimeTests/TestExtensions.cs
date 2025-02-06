// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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