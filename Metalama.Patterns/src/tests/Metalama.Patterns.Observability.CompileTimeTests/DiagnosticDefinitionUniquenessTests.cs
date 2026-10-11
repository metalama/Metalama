// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using System.Reflection;
using Xunit;

namespace Metalama.Patterns.Observability.CompileTimeTests;

/// <summary>
/// Verifies that the diagnostic definitions of <c>Metalama.Patterns.Observability</c> have distinct identifiers.
/// </summary>
/// <remarks>
/// Regression test for https://github.com/metalama/Metalama/issues/2160.
/// </remarks>
public sealed class DiagnosticDefinitionUniquenessTests
{
    /// <summary>
    /// Asserts that no two static fields of type <see cref="IDiagnosticDefinition"/> in the assembly share the same identifier.
    /// </summary>
    [Fact]
    public void DiagnosticIdsAreUnique()
    {
        var definitions = typeof(ObservableAttribute).Assembly
            .GetTypes()
            .SelectMany( t => t.GetFields( BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic ) )
            .Where( f => typeof(IDiagnosticDefinition).IsAssignableFrom( f.FieldType ) )
            .Select( f => (Field: $"{f.DeclaringType!.Name}.{f.Name}", ((IDiagnosticDefinition) f.GetValue( null )!).Id) )
            .ToList();

        Assert.NotEmpty( definitions );

        var duplicates = definitions
            .GroupBy( d => d.Id )
            .Where( g => g.Count() > 1 )
            .Select( g => $"{g.Key}: {string.Join( ", ", g.Select( d => d.Field ) )}" )
            .ToList();

        Assert.Empty( duplicates );
    }
}
