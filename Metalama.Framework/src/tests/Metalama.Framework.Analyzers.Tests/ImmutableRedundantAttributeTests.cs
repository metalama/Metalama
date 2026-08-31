// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Tests of LAMA0886, which reports a marker stating a contract the declaration is bound by anyway.
/// </summary>
/// <remarks>
/// The rule is sound only if it fires exactly where deleting the marker changes nothing, so the assertions come in
/// pairs: one that the marker is reported, and one that the contract is still enforced without it.
/// </remarks>
public sealed class ImmutableRedundantAttributeTests : ImmutableAnalyzerTestBase
{
    [Fact]
    public async Task AMarkerOnATypeThatInheritsTheContractFromABaseType_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            _prologue + "[ImmutableType] class B { } [ImmutableType] class D : B { }",
            "LAMA0886" );

        Assert.Contains( "'D'", message, StringComparison.Ordinal );
        Assert.Contains( "'B'", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task AMarkerOnATypeThatInheritsTheContractFromAnInterface_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            _prologue + "[ImmutableType] interface I { } [ImmutableType] class C : I { }",
            "LAMA0886" );

        Assert.Contains( "'I'", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// The obligation arrives through an unmarked interface, so the message has to name the marked one further up
    /// rather than the one written in the declaration.
    /// </remarks>
    [Fact]
    public async Task AMarkerOnATypeThatInheritsTheContractTransitively_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            _prologue + "[ImmutableType] interface I { } interface J : I { } [ImmutableType] class C : J { }",
            "LAMA0886" );

        Assert.Contains( "'I'", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// The case the rule exists for. An aspect is bound by the contract because <c>IAspect</c> carries the marker, so
    /// marking the aspect as well states something already true, and this is the shape a user is most likely to write.
    /// </remarks>
    [Fact]
    public async Task AMarkerOnAnAspect_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            _prologue + "[ImmutableType] class MyAspect : Metalama.Framework.Aspects.IAspect { }",
            "LAMA0886" );

        Assert.Contains( "IAspect", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// The squiggle has to land on the marker, because deleting it is the entire remedy.
    /// </remarks>
    [Fact]
    public async Task TheDiagnostic_IsReportedOnTheAttribute()
    {
        var code = _prologue + "[ImmutableType] interface I { } [ImmutableType] class C : I { }";
        var diagnostics = await GetDiagnosticsAsync( code );

        var span = Assert.Single( diagnostics ).Location.SourceSpan;

        Assert.Equal( "ImmutableType", code.Substring( span.Start, span.Length ) );
    }

    [Fact]
    public async Task AMarkerOnATypeThatDeclaresTheContractItself_IsNotReported()
        => await AssertNoDiagnosticAsync( _prologue + "[ImmutableType] class C { }" );

    [Fact]
    public async Task AMarkerOnATypeWhoseInterfacesAreUnmarked_IsNotReported()
        => await AssertNoDiagnosticAsync( _prologue + "interface I { } [ImmutableType] class C : I { }" );

    /// <remarks>
    /// An unmarked base type is never silent under the contract, because the base type itself is then not immutable,
    /// so what this asserts is that the rule reported is the one about the base type and not this one.
    /// </remarks>
    [Fact]
    public async Task AMarkerOnATypeWhoseBaseTypeIsUnmarked_IsNotReported()
        => await AssertSingleDiagnosticAsync( _prologue + "class B { } [ImmutableType] class C : B { }", "LAMA0883" );

    /// <remarks>
    /// The control for the pair. Without the marker the contract still binds the type, and the member rules still
    /// report, which is what makes the deletion the rule asks for safe.
    /// </remarks>
    [Fact]
    public async Task ATypeThatInheritsTheContractWithoutRestatingIt_IsStillChecked()
        => await AssertSingleDiagnosticAsync(
            _prologue + "[ImmutableType] interface I { } class C : I { private int[] _a = new int[0]; }",
            "LAMA0882" );

    /// <remarks>
    /// Deleting the marker is safe only if the rest of the analysis does not read it, so a type that both restates the
    /// contract and violates it has to report both rather than one masking the other.
    /// </remarks>
    [Fact]
    public async Task ARedundantMarkerOnATypeThatViolatesTheContract_DoesNotMaskTheViolation()
    {
        var diagnostics = await GetDiagnosticsAsync(
            _prologue + "[ImmutableType] interface I { } [ImmutableType] class C : I { private readonly int[] _a = new int[0]; }" );

        Assert.Equal( new[] { "LAMA0886", "LAMA0882" }, diagnostics.Select( d => d.Id ) );
    }
}
