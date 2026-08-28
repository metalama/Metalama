// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Analyzers.Durability;
using Metalama.Framework.Analyzers.Immutability;
using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Tests of the type-level contract: that every instance field and automatically implemented property of a type
/// marked <c>Durable</c> is itself durable.
/// </summary>
public sealed class DurableTypeAnalyzerTests : DurableAnalyzerTestBase
{
    private const string _preamble = """
                                     using Metalama.Framework.Utilities;
                                     using Microsoft.CodeAnalysis;
                                     using System;
                                     using System.Collections.Generic;
                                     using System.Collections.Immutable;
                                     using System.Runtime.CompilerServices;

                                     """;

    private static string Code( string body ) => _preamble + body;

    #region True positives

    [Fact]
    public async Task FieldOfCompilationBoundType_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "[Durable] class A { private SyntaxTree? _tree; }" ),
            "LAMA0870" );

        Assert.Contains( "A -> _tree -> SyntaxTree", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// This test settles the question the design left open: whether Roslyn exposes the backing field of an
    /// automatically implemented property of a source type in <c>GetMembers</c>. The member walk depends on it, and
    /// the diagnostic must be reported on the property rather than on the invisible backing field.
    /// </remarks>
    [Fact]
    public async Task AutomaticallyImplementedProperty_IsReportedOnTheProperty()
    {
        var diagnostics = await GetDiagnosticsAsync(
            Code( "[Durable] class A { public SyntaxTree? Tree { get; set; } }" ) );

        Assert.Single( diagnostics );
        Assert.Equal( "LAMA0870", diagnostics[0].Id );
        Assert.Contains( "A -> Tree -> SyntaxTree", diagnostics[0].GetMessage(), StringComparison.Ordinal );
    }

    [Fact]
    public async Task PositionalRecord_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "[Durable] record R(ISymbol Symbol);" ),
            "LAMA0870" );

        Assert.Contains( "Symbol", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task FieldOfUnmarkedType_IsReportedWithTheWholeChain()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "[Durable] class A { private List<B>? _items; } class B { }" ),
            "LAMA0870" );

        Assert.Contains( "A -> _items -> List<B> -> B", message, StringComparison.Ordinal );
        Assert.Contains( "not marked [Durable]", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task ImplementationOfDurableInterface_InheritsTheObligation()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "[Durable] interface IContract { } class C : IContract { private SemanticModel? _model; }" ),
            "LAMA0870" );

        Assert.Contains( "SemanticModel", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task NonDurableBaseType_IsReportedOnce()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "class Base { private SyntaxTree? _tree; } [Durable] class A : Base { }" ),
            "LAMA0873" );

        Assert.Contains( "Base", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task DelegateField_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "[Durable] class A { private Func<int>? _factory; }" ),
            "LAMA0870" );

        Assert.Contains( "closure", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task ObjectField_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "[Durable] class A { private object? _value; }" ),
            "LAMA0870" );

        Assert.Contains( "does not constrain what may be stored", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// The remedy is to mark <c>IContract</c>, after which every implementation is verified in turn, which
    /// <see cref="ImplementationOfDurableInterface_InheritsTheObligation"/> covers. The identifier differs from
    /// LAMA0870 because that remedy differs in kind from marking a class, not because the case is undecidable.
    /// </remarks>
    [Fact]
    public async Task UnmarkedInterfaceField_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "[Durable] class A { private IContract? _contract; } interface IContract { }" ),
            "LAMA0876" );

        Assert.Contains( "not marked [Durable]", message, StringComparison.Ordinal );
        Assert.Contains( "every implementation to be durable", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task LazyField_RecommendsDurableLazy()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "[Durable] class A { private Lazy<string>? _value; }" ),
            "LAMA0870" );

        Assert.Contains( "DurableLazy", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task ArrayOfCompilationBoundType_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "[Durable] class A { private SyntaxTree[]? _trees; }" ),
            "LAMA0870" );

        Assert.Contains( "[]", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task TupleElement_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "[Durable] class A { private (string Name, SyntaxTree Tree) _pair; }" ),
            "LAMA0870" );

        Assert.Contains( "SyntaxTree", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task DictionaryValue_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "[Durable] class A { private Dictionary<string, ISymbol>? _map; }" ),
            "LAMA0870" );

        Assert.Contains( "ISymbol", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task ConditionalWeakTableValue_IsReported()
    {
        await AssertSingleDiagnosticAsync(
            Code( "[Durable] class A { private ConditionalWeakTable<string, Compilation>? _cache; }" ),
            "LAMA0870" );
    }

    #endregion

    #region False positives

    [Fact]
    public async Task IntrinsicMembers_AreNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class A { private int _i; private string? _s; private DateTime _d; private Guid _g; private decimal _m; private E _e; } enum E { X }" ) );

    [Fact]
    public async Task Collections0fIntrinsics_AreNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class A { private ImmutableArray<string> _a; private Dictionary<string, int>? _d; private List<int>? _l; }" ) );

    /// <remarks>
    /// The type argument of a weak reference is deliberately not examined, because
    /// <c>ProjectVersionProvider</c> holds a <c>Dictionary&lt;ProjectKey, WeakReference&lt;Compilation&gt;&gt;</c> and
    /// the design document presents that as the recommended shape.
    /// </remarks>
    [Fact]
    public async Task WeakReferenceToCompilation_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class A { private WeakReference<Compilation>? _lastDangerous; }" ) );

    /// <remarks>
    /// The key of a conditional weak table is not kept alive by the table, so it is ignored.
    /// </remarks>
    [Fact]
    public async Task ConditionalWeakTableKeyedByCompilation_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class A { private ConditionalWeakTable<Compilation, string>? _cache; }" ) );

    [Fact]
    public async Task StaticAndConstantMembers_AreNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class A { private static SyntaxTree? _tree; private const string Name = \"x\"; }" ) );

    [Fact]
    public async Task MemberMarkedDurable_WaivesTheDeclaredTypeCheck()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class A { [Durable] private object? _value; [Durable] private IContract? _contract; } interface IContract { }" ) );

    [Fact]
    public async Task SelfReferencingDurableType_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class A { private A? _next; private string? _name; }" ) );

    [Fact]
    public async Task MutuallyRecursiveDurableTypes_AreNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class A { private B? _b; } [Durable] class B { private A? _a; }" ) );

    [Fact]
    public async Task DurableDangerousWrapper_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class A { private DurableDangerous<SyntaxTree> _tree; }" ) );

    [Fact]
    public async Task DurableReference_IsNotReported()
        => await AssertNoDiagnosticAsync(
            "using Metalama.Framework.Utilities; using Metalama.Framework.Code; "
            + "[Durable] class A { private IDurableRef<IDeclaration>? _target; private SerializableDeclarationId _id; }" );

    [Fact]
    public async Task RoslynValueTypes_AreNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class A { private Microsoft.CodeAnalysis.Text.TextSpan _span; private SyntaxAnnotation? _annotation; }" ) );

    [Fact]
    public async Task UnmarkedTypeHoldingACompilation_IsNotReported()
        => await AssertNoDiagnosticAsync( Code( "class A { private SyntaxTree? _tree; }" ) );

    [Fact]
    public async Task ComputedProperty_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class A { private string? _name; public SyntaxTree? Tree => null; }" ) );

    /// <remarks>
    /// The analyzer must neither report nor throw when the attribute is unknown to the compilation, which is the
    /// state of every project that does not reference Metalama.
    /// </remarks>
    [Fact]
    public async Task CompilationWithoutTheAttribute_ProducesNothing()
        => await AssertNoDiagnosticAsync(
            "using Microsoft.CodeAnalysis; class A { private SyntaxTree? _tree; }",
            withMetalamaReference: false );

    [Fact]
    public async Task CodeThatDoesNotCompile_ProducesNothing()
        => await AssertNoDiagnosticAsync( Code( "[Durable] class A { private Undefined? _x; }" ) );

    #endregion
}
