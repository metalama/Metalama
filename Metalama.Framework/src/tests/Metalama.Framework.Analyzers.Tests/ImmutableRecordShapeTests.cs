// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// The shapes whose read-only-ness is not obvious from the source, and the compiler rule that the fix guidance of
/// <c>LAMA0881</c> depends on.
/// </summary>
public sealed class ImmutableRecordShapeTests : ImmutableAnalyzerTestBase
{
    [Fact]
    public async Task PositionalRecordClass_IsNotReported()
        => await AssertNoDiagnosticAsync( _prologue + "[ImmutableObject(true)] record class RC(int X);" );

    [Fact]
    public async Task ReadOnlyRecordStruct_IsNotReported()
        => await AssertNoDiagnosticAsync( _prologue + "[ImmutableObject(true)] readonly record struct RRS(int Z);" );

    /// <remarks>
    /// The trap. A positional <c>record struct</c> generates settable properties, so it violates the contract while
    /// looking exactly like the record class above. The remedy is a different modifier on the declaration rather than
    /// an edit to the member, which is why the message names it.
    /// </remarks>
    [Fact]
    public async Task PositionalRecordStruct_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            _prologue + "[ImmutableObject(true)] record struct RS(int Y);",
            "LAMA0881" );

        Assert.Contains( "readonly record struct", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// The squiggle lands on the name of the primary constructor parameter, which is the only syntax the generated
    /// property has. There is nothing else to point at, and pointing at the type would be misleading, since the type
    /// is not what is wrong.
    /// </remarks>
    [Fact]
    public async Task PositionalRecordStruct_IsReportedOnTheParameter()
    {
        var code = _prologue + "[ImmutableObject(true)] record struct RS(int Y);";
        var diagnostics = await GetDiagnosticsAsync( code );

        var span = Assert.Single( diagnostics ).Location.SourceSpan;

        Assert.Equal( "Y", code.Substring( span.Start, span.Length ) );
    }

    /// <summary>
    /// Asserts the compiler rule that the fix guidance of <c>LAMA0881</c> relies on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rule tells the author to replace <c>set</c> with <c>init</c>, and the campaign that applies it to the
    /// shipped aspects does so on attribute classes whose properties are set as named arguments. That is only a valid
    /// fix because an attribute named argument is an object-initializer assignment, which accepts an <c>init</c>
    /// accessor. If a future language or compiler version changed that, this test would fail rather than the advice
    /// silently becoming wrong.
    /// </para>
    /// <para>
    /// The second half records the boundary: CS0617 requires a read-write property, so an <c>init</c> accessor
    /// without a getter is not a valid named argument. Every property converted by the campaign keeps its getter.
    /// </para>
    /// </remarks>
    [Fact]
    public void InitOnlyProperty_IsAValidAttributeNamedArgument()
    {
        var compilation = CreateCompilation(
            """
            using System;

            class MyAttribute : Attribute
            {
                public string? Name { get; init; }
                public bool Flag { init { } }
            }

            [My( Name = "x" )]
            class WithGetter;

            [My( Flag = true )]
            class WithoutGetter;
            """ );

        var errors = compilation.GetDiagnostics()
            .Where( d => d.Severity == DiagnosticSeverity.Error )
            .ToList();

        var error = Assert.Single( errors );

        Assert.Equal( "CS0617", error.Id );
        Assert.Contains( "Flag", error.GetMessage(), StringComparison.Ordinal );
    }
}
