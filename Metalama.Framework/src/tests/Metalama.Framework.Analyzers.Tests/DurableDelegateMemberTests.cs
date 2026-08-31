// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Tests of the attribute on a member of delegate type, and of LAMA0877, which guards the soundness of the waiver
/// that the attribute grants such a member.
/// </summary>
/// <remarks>
/// A delegate type is never durable, because a delegate holds its target and everything its closure captured, so a
/// durable type cannot declare one by its type alone. An individual delegate may nonetheless be durable, and the
/// assignment shows which: a static method group captures nothing, and a lambda can be read for what it takes. The
/// attribute on the member is what asks for the assignments to be judged instead of the declared type, and that
/// exchange is only worth making where every assignment can be seen.
/// </remarks>
public sealed class DurableDelegateMemberTests : DurableAnalyzerTestBase
{
    private const string _preamble = """
                                     using Metalama.Framework.Utilities;
                                     using Microsoft.CodeAnalysis;
                                     using System;

                                     """;

    private static string Code( string body ) => _preamble + body;

    #region The waiver on a member of delegate type

    /// <remarks>
    /// The control. Without the attribute the declared type is the only evidence, and it says a delegate reaches
    /// anything its closure captured.
    /// </remarks>
    [Fact]
    public async Task ADelegateMemberOfADurableTypeWithoutTheAttribute_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "[Durable] class A { private readonly Func<int>? _f; }" ),
            "LAMA0870" );

        Assert.Contains( "Func<int>", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task ADelegateFieldMarkedWithTheAttribute_IsNotReported()
        => await AssertNoDiagnosticAsync( Code( "[Durable] class A { [Durable] private readonly Func<int>? _f; }" ) );

    [Fact]
    public async Task ADelegatePropertyMarkedWithTheAttribute_IsNotReported()
        => await AssertNoDiagnosticAsync( Code( "[Durable] class A { [Durable] public Func<int>? F { get; } }" ) );

    [Fact]
    public async Task AssigningAStaticMethodGroupToADelegateMember_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class A { [Durable] private Func<int>? _f; public void M() { this._f = Zero; } static int Zero() => 0; }" ) );

    [Fact]
    public async Task AssigningANonCapturingLambdaToADelegateMember_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class A { [Durable] private Func<int>? _f; public void M() { this._f = () => 0; } }" ) );

    /// <remarks>
    /// The point of the whole exercise: the closure holds the syntax tree, which nothing in the declaration shows.
    /// </remarks>
    [Fact]
    public async Task AssigningACapturingLambdaToADelegateMember_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code(
                "[Durable] class A { [Durable] private Func<int>? _f; "
                + "public void M(SyntaxTree tree) { this._f = () => tree.FilePath.Length; } }" ),
            "LAMA0878" );

        Assert.Contains( "tree", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// Combining is how a delegate member is usually given a value, and it stores the closure exactly as a plain
    /// assignment does. Registering only the simple assignment left this silent.
    /// </remarks>
    [Fact]
    public async Task CombiningACapturingLambdaIntoADelegateMember_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code(
                "[Durable] class A { [Durable] private Func<int>? _f; "
                + "public void M(SyntaxTree tree) { this._f += () => tree.FilePath.Length; } }" ),
            "LAMA0878" );

    [Fact]
    public async Task CombiningANonCapturingLambdaIntoADelegateMember_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class A { [Durable] private Func<int>? _f; public void M() { this._f += () => 0; } }" ) );

    /// <remarks>
    /// A compound assignment over a type that is not a delegate has an operand of the member's own type, so widening
    /// the rule to reach the delegate case must not make it say anything here.
    /// </remarks>
    [Fact]
    public async Task CompoundAssignmentToADurableMemberOfAnIntrinsicType_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class A { [Durable] private int _count; public void M() { this._count += 1; } }" ) );

    /// <remarks>
    /// The waiver is what makes a durable type able to hold a callback at all, and the obligation moves to the call
    /// site, which the rule on arguments then checks.
    /// </remarks>
    [Fact]
    public async Task ADelegateMemberInitializedFromADurableParameter_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class A { [Durable] private readonly Func<int> _f; public A([Durable] Func<int> f) { this._f = f; } }" ) );

    #endregion

    #region The visibility of the member, LAMA0877

    /// <remarks>
    /// The whole rule in one case. The waiver replaces a check on the declared type with a check on the assignments,
    /// and an assignment in another assembly is not one the analyzer can check.
    /// </remarks>
    [Fact]
    public async Task ADurableMemberWithAPublicSetter_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "class A { [Durable] public Func<int>? F { get; set; } }" ),
            "LAMA0877" );

        Assert.Contains( "F", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// The divergence from the sibling immutability contract, which accepts <c>init</c>. It confines an assignment to
    /// construction, which is all immutability asks, but the object initializer that performs it may sit in any
    /// assembly, so it confines an assignment to nothing the analyzer can look at.
    /// </remarks>
    [Fact]
    public async Task ADurableMemberWithAPublicInitAccessor_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code( "class A { [Durable] public Func<int>? F { get; init; } }" ),
            "LAMA0877" );

    [Fact]
    public async Task ADurablePublicField_IsReported()
        => await AssertSingleDiagnosticAsync( Code( "class A { [Durable] public Func<int>? F; }" ), "LAMA0877" );

    /// <remarks>
    /// Internal is not private. An assembly that grants itself access with <c>InternalsVisibleTo</c> writes the member
    /// from a compilation this analysis never sees.
    /// </remarks>
    [Fact]
    public async Task ADurableInternalField_IsReported()
        => await AssertSingleDiagnosticAsync( Code( "class A { [Durable] internal Func<int>? F; }" ), "LAMA0877" );

    [Fact]
    public async Task ADurablePrivateField_IsNotReported()
        => await AssertNoDiagnosticAsync( Code( "class A { [Durable] private Func<int>? _f; }" ) );

    /// <remarks>
    /// Read-only is the other way to confine every assignment to the declaring type, and the compiler checks it, so a
    /// public read-only field is as verifiable as a private one.
    /// </remarks>
    [Fact]
    public async Task ADurablePublicReadOnlyField_IsNotReported()
        => await AssertNoDiagnosticAsync( Code( "class A { [Durable] public readonly Func<int>? F; }" ) );

    [Fact]
    public async Task ADurableMemberWithAPrivateSetter_IsNotReported()
        => await AssertNoDiagnosticAsync( Code( "class A { [Durable] public Func<int>? F { get; private set; } }" ) );

    [Fact]
    public async Task ADurableGetOnlyProperty_IsNotReported()
        => await AssertNoDiagnosticAsync( Code( "class A { [Durable] public Func<int>? F { get; } }" ) );

    /// <remarks>
    /// The rules on assignments check a marked member whatever type declares it, so this one has to reach there too.
    /// None of the types in this region carry the attribute, which is what that asserts.
    /// </remarks>
    [Fact]
    public async Task ADurableMemberOfATypeThatIsNotItselfDurable_IsReported()
        => await AssertSingleDiagnosticAsync( Code( "class A { [Durable] public Func<int>? F; }" ), "LAMA0877" );

    /// <remarks>
    /// A member without the attribute states nothing about the values assigned to it, so its visibility is not this
    /// rule's business.
    /// </remarks>
    [Fact]
    public async Task APublicMemberWithoutTheAttribute_IsNotReported()
        => await AssertNoDiagnosticAsync( Code( "class A { public Func<int>? F { get; set; } }" ) );

    #endregion
}
