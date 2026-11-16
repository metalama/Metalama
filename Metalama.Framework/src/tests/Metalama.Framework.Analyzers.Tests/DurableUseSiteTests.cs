// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Analyzers.Durability;
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

    #region Anonymous objects and collection initializers, LAMA0871 and LAMA0872

    private static string KeepCode( string body )
        => Code(
            "using System.Collections.Generic; class A { static void Keep([Durable] object value) { } "
            + "[Durable] private object? _value; " + body + " }" );

    /// <remarks>
    /// The shape that the interceptor API documents for <c>WithArgs</c>. See #2343.
    /// </remarks>
    [Fact]
    public async Task PassingAnAnonymousObjectOfDurableValues_IsNotReported()
        => await AssertNoDiagnosticAsync( KeepCode( "static void M() { Keep( new { category = \"Sales\", weight = 2 } ); }" ) );

    [Fact]
    public async Task PassingAnAnonymousObjectWithACompilationBoundValue_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            KeepCode( "static void M(ISymbol s) { Keep( new { name = \"x\", symbol = s } ); }" ),
            "LAMA0872" );

        Assert.Contains( ".symbol", message, StringComparison.Ordinal );
        Assert.Contains( "ISymbol", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task AssigningAnAnonymousObjectOfDurableValues_IsNotReported()
        => await AssertNoDiagnosticAsync( KeepCode( "void M() { this._value = new { a = 1, b = new[] { \"x\" } }; }" ) );

    /// <remarks>
    /// The type rule: the value reaches the parameter through a local, so only its anonymous type is visible.
    /// </remarks>
    [Fact]
    public async Task PassingAnAnonymousObjectThroughALocal_IsNotReported()
        => await AssertNoDiagnosticAsync( KeepCode( "static void M() { var args = new { a = 1, b = \"x\" }; Keep( args ); }" ) );

    [Fact]
    public async Task PassingAnAnonymousObjectWithACompilationBoundValueThroughALocal_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            KeepCode( "static void M(SyntaxTree t) { var args = new { a = 1, tree = t }; Keep( args ); }" ),
            "LAMA0872" );

        Assert.Contains( ".tree", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// The expression rule: the property is of type <see cref="object"/>, which the type rule cannot verify, but its
    /// initializer is a literal.
    /// </remarks>
    [Fact]
    public async Task PassingAnAnonymousObjectWithAnObjectPropertyInitializedWithALiteral_IsNotReported()
        => await AssertNoDiagnosticAsync( KeepCode( "static void M() { Keep( new { a = (object) 1 } ); }" ) );

    [Fact]
    public async Task PassingANestedAnonymousObjectWithACompilationBoundValue_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            KeepCode( "static void M(Compilation c) { Keep( new { outer = new { inner = c } } ); }" ),
            "LAMA0872" );

        Assert.Contains( ".outer -> .inner", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task PassingAnAnonymousObjectWithALambdaThatCapturesACompilationBoundValue_IsReported()
        => await AssertSingleDiagnosticAsync(
            KeepCode( "static void M(Compilation c) { Keep( new { f = (Func<string?>) (() => c.AssemblyName) } ); }" ),
            "LAMA0878" );

    [Fact]
    public async Task PassingADictionaryWithAnIndexInitializerOfDurableValues_IsNotReported()
        => await AssertNoDiagnosticAsync(
            KeepCode( "static void M() { Keep( new Dictionary<string, object?> { [\"category\"] = \"Sales\", [\"weight\"] = 2 } ); }" ) );

    [Fact]
    public async Task PassingADictionaryWithACollectionInitializerOfDurableValues_IsNotReported()
        => await AssertNoDiagnosticAsync(
            KeepCode( "static void M() { Keep( new Dictionary<string, object?> { { \"category\", \"Sales\" }, { \"weight\", 2 } } ); }" ) );

    [Fact]
    public async Task PassingAListWithACollectionInitializerOfDurableValues_IsNotReported()
        => await AssertNoDiagnosticAsync( KeepCode( "static void M() { Keep( new List<object> { 1, \"x\", new { a = 2 } } ); }" ) );

    [Fact]
    public async Task PassingADictionaryWithACompilationBoundValue_IsReported()
        => await AssertSingleDiagnosticAsync(
            KeepCode( "static void M(ISymbol s) { Keep( new Dictionary<string, object?> { [\"symbol\"] = s } ); }" ),
            "LAMA0872" );

    [Fact]
    public async Task PassingADictionaryWithACompilationBoundKey_IsReported()
        => await AssertSingleDiagnosticAsync(
            KeepCode( "static void M(ISymbol s) { Keep( new Dictionary<object, int> { { s, 1 } } ); }" ),
            "LAMA0872" );

    /// <remarks>
    /// The constructor can store its arguments, here a collection to copy, so they are evaluated as well.
    /// </remarks>
    [Fact]
    public async Task PassingAListCopiedFromANonDurableCollection_IsReported()
        => await AssertSingleDiagnosticAsync(
            KeepCode( "static void M(IEnumerable<object> source) { Keep( new List<object>( source ) { 1 } ); }" ),
            "LAMA0872" );

    /// <remarks>
    /// The rule applies only to the system collections whose durability depends on their type arguments. Another type
    /// with an initializer is judged by its type.
    /// </remarks>
    [Fact]
    public async Task PassingAnUnmarkedTypeWithAnInitializer_IsReported()
        => await AssertSingleDiagnosticAsync(
            KeepCode( "class Bag : List<object> { } static void M() { Keep( new Bag { 1 } ); }" ),
            "LAMA0872" );

    /// <remarks>
    /// The rule applies only when the creation expression is the value itself. Through a local, the collection may have
    /// received other elements, so the type argument is the evidence.
    /// </remarks>
    [Fact]
    public async Task PassingADictionaryThroughALocal_IsReported()
        => await AssertSingleDiagnosticAsync(
            KeepCode( "static void M() { var d = new Dictionary<string, object?> { [\"a\"] = 1 }; Keep( d ); }" ),
            "LAMA0872" );

    #endregion

    #region Suggestion of a durable conversion

    private static string CodeModelCode( string body )
        => Code(
            "using Metalama.Framework.Code; class A { static void Keep([Durable] object value) { } "
            + "[Durable] private object? _value; " + body + " }" );

    [Fact]
    public async Task PassingAnAnonymousObjectWithATypeProperty_SuggestsToDurableRef()
    {
        var diagnostics = await GetDiagnosticsAsync( CodeModelCode( "static void M(IType type) { Keep( new { T = type } ); }" ) );

        var diagnostic = Assert.Single( diagnostics );
        Assert.Equal( "LAMA0872", diagnostic.Id );
        Assert.Contains( "Call 'ToDurableRef()' on 'type'", diagnostic.GetMessage(), StringComparison.Ordinal );
        Assert.Equal( "ToDurableRef", diagnostic.Properties[DurableContractAnalyzer.DurableConversionPropertyName] );

        // The diagnostic is reported on the value that is not durable rather than on the whole argument.
        Assert.Equal( "type", diagnostic.Location.SourceTree!.GetText( TestContext.Current.CancellationToken ).ToString( diagnostic.Location.SourceSpan ) );
    }

    [Fact]
    public async Task PassingAnExpression_SuggestsToDurable()
    {
        var message = await AssertSingleDiagnosticAsync(
            CodeModelCode( "static void M(IExpression expression) { Keep( expression ); }" ),
            "LAMA0872" );

        Assert.Contains( "Call 'ToDurable()' on 'expression'", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// A parameter is both a declaration and an expression. The durable reference is preferred, because it keeps the
    /// declaration, whereas the durable expression keeps only its text.
    /// </remarks>
    [Fact]
    public async Task PassingAParameter_SuggestsToDurableRef()
    {
        var message = await AssertSingleDiagnosticAsync(
            CodeModelCode( "static void M(IParameter parameter) { Keep( new { requestId = parameter } ); }" ),
            "LAMA0872" );

        Assert.Contains( "Call 'ToDurableRef()' on 'parameter'", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task AssigningADeclaration_SuggestsToDurableRef()
    {
        var message = await AssertSingleDiagnosticAsync(
            CodeModelCode( "void M(IMethod method) { this._value = method; }" ),
            "LAMA0871" );

        Assert.Contains( "Call 'ToDurableRef()' on 'method'", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task PassingACompilation_DoesNotSuggestAConversion()
    {
        var diagnostics = await GetDiagnosticsAsync( CodeModelCode( "static void M(Compilation c) { Keep( c ); }" ) );

        var diagnostic = Assert.Single( diagnostics );
        Assert.DoesNotContain( "Call '", diagnostic.GetMessage(), StringComparison.Ordinal );
        Assert.False( diagnostic.Properties.ContainsKey( DurableContractAnalyzer.DurableConversionPropertyName ) );
    }

    /// <remarks>
    /// The conversion is an extension method, so it is suggested only where its namespace is imported.
    /// </remarks>
    [Fact]
    public async Task PassingATypeWhereTheExtensionIsNotImported_DoesNotSuggestAConversion()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "class A { static void Keep([Durable] object value) { } static void M(Metalama.Framework.Code.IType type) { Keep( type ); } }" ),
            "LAMA0872" );

        Assert.DoesNotContain( "Call '", message, StringComparison.Ordinal );
    }

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
