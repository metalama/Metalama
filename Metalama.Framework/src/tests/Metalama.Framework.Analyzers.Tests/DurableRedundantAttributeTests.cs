// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Tests of LAMA0874, which reports an attribute stating a contract the declaration is bound by anyway.
/// </summary>
/// <remarks>
/// The rule is sound only if it fires exactly where deleting the attribute changes nothing, so the assertions come in
/// pairs: one that the attribute is reported, and one that the contract is still enforced without it.
/// </remarks>
public sealed class DurableRedundantAttributeTests : DurableAnalyzerTestBase
{
    private const string _preamble = """
                                     using Metalama.Framework.Utilities;
                                     using Microsoft.CodeAnalysis;
                                     using System;

                                     """;

    private static string Code( string body ) => _preamble + body;

    [Fact]
    public async Task AnAttributeOnATypeThatInheritsTheContractFromABaseType_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "[Durable] class B { } [Durable] class D : B { }" ),
            "LAMA0874" );

        Assert.Contains( "'D'", message, StringComparison.Ordinal );
        Assert.Contains( "'B'", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task AnAttributeOnATypeThatInheritsTheContractFromAnInterface_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "[Durable] interface I { } [Durable] class C : I { }" ),
            "LAMA0874" );

        Assert.Contains( "'I'", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// An interface inherits the obligation from an interface it extends exactly as a class does, and there is no base
    /// type to find it on, so this exercises the other half of the walk.
    /// </remarks>
    [Fact]
    public async Task AnAttributeOnAnInterfaceThatExtendsAMarkedInterface_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code( "[Durable] interface I { } [Durable] interface J : I { }" ),
            "LAMA0874" );

    /// <remarks>
    /// The obligation arrives through an unmarked interface, so the message has to name the marked one further up
    /// rather than the one written in the declaration, which requires more than looking at the direct interfaces.
    /// </remarks>
    [Fact]
    public async Task AnAttributeOnATypeThatInheritsTheContractTransitively_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "[Durable] interface I { } interface J : I { } [Durable] class C : J { }" ),
            "LAMA0874" );

        Assert.Contains( "'I'", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// The squiggle has to land on the attribute, because deleting it is the entire remedy. Naming the type instead
    /// would fade or underline a declaration that is not what has to change.
    /// </remarks>
    [Fact]
    public async Task TheDiagnostic_IsReportedOnTheAttribute()
    {
        var code = Code( "[Durable] interface I { } [Durable] class C : I { }" );
        var diagnostics = await GetDiagnosticsAsync( code );

        var span = Assert.Single( diagnostics ).Location.SourceSpan;

        Assert.Equal( "Durable", code.Substring( span.Start, span.Length ) );
    }

    [Fact]
    public async Task AnAttributeOnATypeThatDeclaresTheContractItself_IsNotReported()
        => await AssertNoDiagnosticAsync( Code( "[Durable] class C { }" ) );

    [Fact]
    public async Task AnAttributeOnATypeWhoseInterfacesAreUnmarked_IsNotReported()
        => await AssertNoDiagnosticAsync( Code( "interface I { } [Durable] class C : I { }" ) );

    /// <remarks>
    /// An unmarked base type is never silent under the contract, because the base type itself is then not durable, so
    /// what this asserts is that the rule reported is the one about the base type and not this one.
    /// </remarks>
    [Fact]
    public async Task AnAttributeOnATypeWhoseBaseTypeIsUnmarked_IsNotReported()
        => await AssertSingleDiagnosticAsync( Code( "class B { } [Durable] class C : B { }" ), "LAMA0873" );

    /// <remarks>
    /// The control for the pair. Without the attribute the contract still binds the type, and the member rules still
    /// report, which is what makes the deletion the rule asks for safe.
    /// </remarks>
    [Fact]
    public async Task ATypeThatInheritsTheContractWithoutRestatingIt_IsStillChecked()
        => await AssertSingleDiagnosticAsync(
            Code( "[Durable] interface I { } class C : I { private readonly SyntaxTree? _tree; }" ),
            "LAMA0870" );

    /// <remarks>
    /// Deleting the attribute is safe only if the rest of the analysis does not read it. Here the type both restates
    /// the contract and violates it, so the redundancy and the violation have to be reported together rather than one
    /// masking the other.
    /// </remarks>
    [Fact]
    public async Task ARedundantAttributeOnATypeThatViolatesTheContract_DoesNotMaskTheViolation()
    {
        var diagnostics = await GetDiagnosticsAsync(
            Code( "[Durable] interface I { } [Durable] class C : I { private readonly SyntaxTree? _tree; }" ) );

        Assert.Equal( new[] { "LAMA0874", "LAMA0870" }, diagnostics.Select( d => d.Id ) );
    }
}
