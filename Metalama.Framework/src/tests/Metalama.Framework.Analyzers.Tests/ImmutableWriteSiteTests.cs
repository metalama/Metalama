// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// The rules that accept a member whose write access is private and check its assignments instead, reporting at the
/// assignment rather than at the declaration.
/// </summary>
public sealed class ImmutableWriteSiteTests : ImmutableAnalyzerTestBase
{
    private const string _wrap = "[ImmutableType] class C {{ {0} }}";

    private static string Code( string body ) => _prologue + string.Format( _wrap, body );

    // ------------------------------------------------------------------------------------------------------------
    // Where a write is allowed.
    // ------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task WriteInAConstructor_IsNotReported()
        => await AssertNoDiagnosticAsync( Code( "private int _count; public C() { this._count = 1; }" ) );

    [Fact]
    public async Task WriteInAnInitAccessor_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "private int _count; public int Count { get => this._count; init => this._count = value; }" ) );

    [Fact]
    public async Task FieldInitializer_IsNotReported()
        => await AssertNoDiagnosticAsync( Code( "private int _count = 1;" ) );

    [Fact]
    public async Task PrivateSetterAssignedInAConstructor_IsNotReported()
        => await AssertNoDiagnosticAsync( Code( "public int Count { get; private set; } public C() { this.Count = 1; }" ) );

    [Fact]
    public async Task ReadingAMember_IsNotReported()
        => await AssertNoDiagnosticAsync( Code( "private int _count; public int Get() => this._count;" ) );

    // ------------------------------------------------------------------------------------------------------------
    // Where it is not.
    // ------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task WriteInAMethod_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "private int _count; public void M() { this._count = 1; }" ),
            "LAMA0887" );

        Assert.Contains( "must be written in an immutable style", message, StringComparison.Ordinal );
        Assert.Contains( "outside a constructor", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// The diagnostic must land on the assignment, not on the declaration, because that is the line that has to
    /// change.
    /// </remarks>
    [Fact]
    public async Task TheDiagnostic_IsReportedAtTheWrite()
    {
        var code = Code( "private int _count; public void M() { this._count = 1; }" );
        var diagnostics = await GetDiagnosticsAsync( code );

        var span = Assert.Single( diagnostics ).Location.SourceSpan;

        Assert.Equal( "this._count", code.Substring( span.Start, span.Length ) );
    }

    [Fact]
    public async Task PrivateSetterAssignedInAMethod_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code( "public int Count { get; private set; } public void M() { this.Count = 1; }" ),
            "LAMA0887" );

    [Theory]
    [InlineData( "this._count += 1;" )]
    [InlineData( "this._count++;" )]
    [InlineData( "this._count--;" )]
    [InlineData( "++this._count;" )]
    public async Task CompoundWritesAndIncrements_AreReported( string statement )
        => await AssertSingleDiagnosticAsync(
            Code( $"private int _count; public void M() {{ {statement} }}" ),
            "LAMA0887" );

    /// <remarks>
    /// A lambda written inside a constructor does not run there. The delegate may be stored and invoked at any later
    /// time, so an assignment in its body is not part of construction.
    /// </remarks>
    [Fact]
    public async Task WriteInALambdaInsideAConstructor_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code( "private int _count; public C() { Action a = () => this._count = 1; a(); }" ),
            "LAMA0887" );

    [Fact]
    public async Task WriteInALocalFunctionInsideAConstructor_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code( "private int _count; public C() { void Set() { this._count = 1; } Set(); }" ),
            "LAMA0887" );

    /// <remarks>
    /// A write to another instance of the same type is still a write to a member of an immutable type.
    /// </remarks>
    [Fact]
    public async Task WriteToAnotherInstance_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code( "private int _count; public void M(C other) { other._count = 1; }" ),
            "LAMA0887" );

    /// <remarks>
    /// A deconstruction assigns every element of its target, and none of those assignments is a simple assignment.
    /// </remarks>
    [Fact]
    public async Task DeconstructionIntoMembers_IsReported()
    {
        var diagnostics = await GetDiagnosticsAsync(
            Code( "private int _x; private int _y; public void M() { (this._x, this._y) = (1, 2); }" ) );

        Assert.Equal( 2, diagnostics.Length );
        Assert.All( diagnostics, d => Assert.Equal( "LAMA0887", d.Id ) );
    }

    [Fact]
    public async Task DeconstructionWithADiscard_ReportsOnlyTheMember()
        => await AssertSingleDiagnosticAsync(
            Code( "private int _x; public void M() { (this._x, _) = (1, 2); }" ),
            "LAMA0887" );

    [Fact]
    public async Task DeconstructionInAConstructor_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "private int _x; private int _y; public C() { (this._x, this._y) = (1, 2); }" ) );

    [Fact]
    public async Task DeconstructionIntoLocals_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "private readonly int _x; public void M() { var (a, b) = (1, 2); }" ) );

    // ------------------------------------------------------------------------------------------------------------
    // ref and out, which are writes that do not look like assignments.
    // ------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task PassingAMemberAsOut_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "private int _count; public void M() { Set(out this._count); } static void Set(out int x) => x = 1;" ),
            "LAMA0888" );

        Assert.Contains( "'out' argument", message, StringComparison.Ordinal );
        Assert.Contains( "which writes it", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// A ref argument may only read. Volatile.Read( ref field ) passes a member by reference and stores nothing, and
    /// a declared signature does not say which of the two a call does, so silence is preferred to a finding that is
    /// wrong wherever that idiom appears. DurableLazy is the concrete case that made this necessary.
    /// </remarks>
    [Fact]
    public async Task PassingAMemberAsRef_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "private int _count; public void M() { Set(ref this._count); } static void Set(ref int x) => x = 1;" ) );

    [Fact]
    public async Task PassingAMemberByValue_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "private int _count; public void M() { Set(this._count); } static void Set(int x) { }" ) );

    /// <remarks>
    /// A <c>ref</c> or <c>out</c> argument in a constructor is part of construction, like any other write there.
    /// </remarks>
    [Fact]
    public async Task PassingAMemberAsOutInAConstructor_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "private int _count; public C() { Set(out this._count); } static void Set(out int x) => x = 1;" ) );

    // ------------------------------------------------------------------------------------------------------------
    // Controls.
    // ------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task WriteToAMemberOfAnUnboundType_IsNotReported()
        => await AssertNoDiagnosticAsync(
            _prologue + "class Other { private int _count; public void M() { this._count = 1; } }" );

    [Fact]
    public async Task WriteToAStaticField_IsNotReported()
        => await AssertNoDiagnosticAsync( Code( "private static int _count; public void M() { _count = 1; }" ) );

    /// <remarks>
    /// An advice member is code injected into the target, not state of the aspect, so writing it is not a defect.
    /// The declaration rules waive it for the same reason.
    /// </remarks>
    [Fact]
    public async Task WriteToAnAdviceMember_IsNotReported()
        => await AssertNoDiagnosticAsync(
            _prologue
            + "using Metalama.Framework.Aspects;\n"
            + "[ImmutableType] class C { [Introduce] private int _count; public void M() { this._count = 1; } }" );

    /// <remarks>
    /// A property with a body is not state of its own. What it assigns is a field, and that assignment is reported
    /// inside the accessor, which is the better place, so reporting the property write too would double-count.
    /// </remarks>
    [Fact]
    public async Task WriteThroughAPropertyWithABody_IsReportedOnceInTheAccessor()
    {
        var diagnostics = await GetDiagnosticsAsync(
            Code(
                "private int _count; "
                + "public int Count { get => this._count; set => this._count = value; } "
                + "public void M() { this.Count = 1; }" ) );

        var diagnostic = Assert.Single( diagnostics );

        Assert.Equal( "LAMA0887", diagnostic.Id );
        Assert.Contains( "_count", diagnostic.GetMessage(), StringComparison.Ordinal );
    }
}
