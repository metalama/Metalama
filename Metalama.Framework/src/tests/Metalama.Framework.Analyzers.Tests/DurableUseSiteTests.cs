// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Tests of the rules that apply where a value is stored or passed: LAMA0871 for an assignment to a durable member,
/// LAMA0872 for an argument to a durable parameter, and LAMA0878 for what a lambda captures at either site.
/// </summary>
public sealed class DurableUseSiteTests : DurableAnalyzerTestBase
{
    private const string _preamble = """
                                     using Metalama.Framework.Utilities;
                                     using Microsoft.CodeAnalysis;
                                     using System;

                                     """;

    private static string Code( string body ) => _preamble + body;

    #region Assignment, LAMA0871

    [Fact]
    public async Task AssigningACompilationBoundValueToADurableMember_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "class A { [Durable] private object? _value; public void M(SyntaxTree tree) { this._value = tree; } }" ),
            "LAMA0871" );

        Assert.Contains( "_value", message, StringComparison.Ordinal );
        Assert.Contains( "SyntaxTree", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task AssigningToADurableProperty_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code( "class A { [Durable] public object? Value { get; private set; } public void M(ISymbol s) { this.Value = s; } }" ),
            "LAMA0871" );

    [Fact]
    public async Task DurableFieldInitializedWithACompilationBoundValue_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code( "class A { [Durable] private object? _value = Holder.Tree; } static class Holder { public static SyntaxTree? Tree; }" ),
            "LAMA0871" );

    /// <remarks>
    /// <para>
    /// A lambda that mentions nothing of its surroundings holds nothing, and the compiler gives it a cached static
    /// delegate. The rule used to report one anyway when it was written inside a local function that captured a
    /// variable, because <c>DataFlowAnalysis.Captured</c> names every variable captured by a closure the region takes
    /// part in, including the closure of the enclosing local function.
    /// </para>
    /// <para>
    /// This is the shape that produced the false positive, in <c>MulticastImplementation</c>: a list of rules built by
    /// local functions, with lambdas passed to a durable parameter from inside them.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task ALambdaInALocalFunctionThatCapturesNothingItself_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code(
                "using System.Collections.Generic; class A { public static void Take( [Durable] Func<int, bool> f ) { } "
                + "public void M() { var rules = new List<Action<SyntaxTree>>(); "
                + "void Accept() { rules.Add( t => { } ); Take( x => x > 0 ); } Accept(); } }" ) );

    /// <remarks>
    /// The control for the test above: the same shape, but the lambda does mention the captured variable.
    /// </remarks>
    [Fact]
    public async Task ALambdaInALocalFunctionThatCapturesTheVariableItself_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code(
                "using System.Collections.Generic; class A { public static void Take( [Durable] Func<int, bool> f ) { } "
                + "public void M() { var rules = new List<Action<SyntaxTree>>(); "
                + "void Accept() { rules.Add( t => { } ); Take( x => rules.Count > x ); } Accept(); } }" ),
            "LAMA0878" );

    [Fact]
    public async Task AssigningADurableValue_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "class A { [Durable] private object? _value; public void M(string s) { this._value = s; } }" ) );

    /// <remarks>
    /// The lock-object idiom. A bare <see cref="object"/> reaches nothing, but its declared type cannot say so, and
    /// without this every durable type that needs a lock would be unable to state its contract. <c>DurableLazy</c>
    /// depends on it.
    /// </remarks>
    [Fact]
    public async Task AssigningANewObject_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "class A { [Durable] private readonly object _lock = new object(); }" ) );

    [Fact]
    public async Task AssigningNull_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "class A { [Durable] private object? _value; public void M() { this._value = null; } }" ) );

    /// <remarks>
    /// The obligation was discharged where the value entered, so it is not imposed again where it moves. Without this
    /// rule <c>DurableLazy</c> could not assign its checked constructor parameter to its own durable field.
    /// </remarks>
    [Fact]
    public async Task ForwardingADurableParameter_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "class A { [Durable] private object? _value; public A([Durable] object value) { this._value = value; } }" ) );

    /// <remarks>
    /// The idiom by which a constructor forwards a checked parameter into a durable field. Found by building the
    /// product rather than by reasoning: <c>DurableLazy</c> writes exactly this, and the rule reported it until the
    /// expression walk learned to look through a coalescing operator.
    /// </remarks>
    [Fact]
    public async Task ForwardingADurableParameterThroughANullCheck_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code(
                "class A { [Durable] private Func<int>? _value; "
                + "public A([Durable] Func<int> value) { this._value = value ?? throw new ArgumentNullException(nameof(value)); } }" ) );

    [Fact]
    public async Task ConditionalExpressionWithANonDurableBranch_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code( "class A { [Durable] private object? _value; public void M(bool b, ISymbol s) { this._value = b ? \"x\" : (object) s; } }" ),
            "LAMA0871" );

    #endregion

    #region Argument, LAMA0872

    [Fact]
    public async Task PassingACompilationBoundArgumentToADurableParameter_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "class A { static void Keep([Durable] object value) { } static void M(Compilation c) { Keep(c); } }" ),
            "LAMA0872" );

        Assert.Contains( "value", message, StringComparison.Ordinal );
        Assert.Contains( "Compilation", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task PassingByNamedArgument_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code( "class A { static void Keep(int i, [Durable] object value) { } static void M(ISymbol s) { Keep(value: s, i: 1); } }" ),
            "LAMA0872" );

    [Fact]
    public async Task PassingToADurableConstructorParameter_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code( "class Box { public Box([Durable] object value) { } } class A { static object M(SemanticModel m) => new Box(m); }" ),
            "LAMA0872" );

    [Fact]
    public async Task PassingADurableArgument_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "class A { static void Keep([Durable] object value) { } static void M() { Keep(\"x\"); } }" ) );

    [Fact]
    public async Task PassingToAnUnmarkedParameter_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "class A { static void Keep(object value) { } static void M(Compilation c) { Keep(c); } }" ) );

    #endregion

    #region Closures, LAMA0878

    [Fact]
    public async Task LambdaCapturingACompilation_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "class A { static void Keep([Durable] Func<int> f) { } static void M(Compilation c) { Keep(() => c.GetHashCode()); } }" ),
            "LAMA0878" );

        Assert.Contains( "'c'", message, StringComparison.Ordinal );
        Assert.Contains( "Compilation", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task LambdaCapturingAValueThatReachesASymbol_ReportsTheChain()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code(
                "class Holder { public ISymbol? Symbol; } "
                + "class A { static void Keep([Durable] Func<int> f) { } static void M(Holder h) { Keep(() => h.GetHashCode()); } }" ),
            "LAMA0878" );

        Assert.Contains( "h -> Holder", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task LambdaCapturingThis_IsReportedAsThis()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code(
                "class A { private SyntaxTree? _tree; static void Keep([Durable] Func<int> f) { } "
                + "void M() { Keep(() => this._tree!.GetHashCode()); } }" ),
            "LAMA0878" );

        Assert.Contains( "this", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task DurableLazyConstructedWithACapturingLambda_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code( "class A { static object M(ISymbol s) => new DurableLazy<string>(() => s.Name); }" ),
            "LAMA0878" );

    [Fact]
    public async Task LambdaCapturingNothing_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "class A { static void Keep([Durable] Func<int> f) { } static void M() { Keep(() => 42); } }" ) );

    [Fact]
    public async Task LambdaCapturingOnlyDurableValues_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "class A { static void Keep([Durable] Func<int> f) { } static void M(string s) { Keep(() => s.Length); } }" ) );

    /// <remarks>
    /// The parameters of the lambda are not captured, so their durability is irrelevant.
    /// </remarks>
    [Fact]
    public async Task LambdaWithNonDurableParameters_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "class A { static void Keep([Durable] Func<Compilation, int> f) { } static void M() { Keep(c => c.GetHashCode()); } }" ) );

    [Fact]
    public async Task StaticMethodGroup_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "class A { static int Zero() => 0; static void Keep([Durable] Func<int> f) { } static void M() { Keep(Zero); } }" ) );

    [Fact]
    public async Task DurableLazyWithANonCapturingLambda_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "class A { static object M() => new DurableLazy<string>(() => \"x\"); }" ) );

    /// <remarks>
    /// A delegate that arrives through a local is not visible at the use site, so the declared type is the only
    /// evidence and the delegate rule applies. This records the known limit rather than a defect.
    /// </remarks>
    [Fact]
    public async Task DelegateArrivingThroughALocal_FallsBackToTheDeclaredType()
        => await AssertSingleDiagnosticAsync(
            Code(
                "class A { static void Keep([Durable] Func<int> f) { } "
                + "static void M(Compilation c) { Func<int> local = () => c.GetHashCode(); Keep(local); } }" ),
            "LAMA0872" );

    #endregion

    /// <remarks>
    /// A user-defined conversion produces an object of the target type, about which the operand says nothing. Erasing
    /// the conversion read the verdict of the operand, so a durable operand hid a result that is not durable.
    /// </remarks>
    [Fact]
    public async Task AUserDefinedConversionFromADurableOperand_IsReportedOnItsResult()
        => await AssertSingleDiagnosticAsync(
            Code(
                "class Wrapper { public SyntaxTree? Tree; public static implicit operator Wrapper( string s ) => new(); } "
                + "class A { [Durable] private Wrapper? _w; public void M() { this._w = \"x\"; } }" ),
            "LAMA0871" );
}
