// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Tests that a durable generic type is trusted only for the type arguments it does not store.
/// </summary>
/// <remarks>
/// Requiring every type argument to be durable would be simpler and wrong: the first exception would be
/// <c>IDurableRef&lt;T&gt;</c>, whose type argument is a phantom because it stores only a serializable identifier.
/// </remarks>
public sealed class DurableGenericArgumentTests : DurableAnalyzerTestBase
{
    private const string _preamble = """
                                     using Metalama.Framework.Utilities;
                                     using Microsoft.CodeAnalysis;
                                     using System;

                                     """;

    private static string Code( string body ) => _preamble + body;

    [Fact]
    public async Task StoredTypeArgumentThatIsNotDurable_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "[Durable] class Box<T> { private T? _value; } [Durable] class A { private Box<ISymbol>? _box; }" ),
            "LAMA0870" );

        Assert.Contains( "Box<ISymbol>", message, StringComparison.Ordinal );
        Assert.Contains( "ISymbol", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task DurableLazyOfACompilationBoundType_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code( "[Durable] class A { private DurableLazy<SyntaxTree>? _lazy; }" ),
            "LAMA0870" );

    [Fact]
    public async Task StoredTypeArgumentThatIsDurable_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class Box<T> { private T? _value; } [Durable] class A { private Box<string>? _box; }" ) );

    /// <remarks>
    /// The type parameter is a phantom: nothing of type <c>T</c> is stored, so a construction may supply anything.
    /// This is the shape of <c>IDurableRef&lt;T&gt;</c>.
    /// </remarks>
    [Fact]
    public async Task PhantomTypeArgument_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class Id<T> { private string? _id; } [Durable] class A { private Id<ISymbol>? _id; }" ) );

    [Fact]
    public async Task RealDurableReference_IsNotReported()
        => await AssertNoDiagnosticAsync(
            "using Metalama.Framework.Utilities; using Metalama.Framework.Code; "
            + "[Durable] class A { private IDurableRef<IDeclaration>? _target; }" );

    [Fact]
    public async Task TypeArgumentNestedInACollection_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code(
                "using System.Collections.Generic; "
                + "[Durable] class Box<T> { private List<T>? _values; } [Durable] class A { private Box<ISymbol>? _box; }" ),
            "LAMA0870" );

    [Fact]
    public async Task SelfReferencingGenericType_DoesNotDiverge()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class Node<T> { private Node<T>? _next; private T? _value; } [Durable] class A { private Node<string>? _n; }" ) );

    /// <remarks>
    /// A contravariant parameter appears only in input position, so an implementation never receives a value of that
    /// type to store. Without this, <c>IEligibilityRule&lt;in T&gt;</c> and <c>IAnnotation&lt;in T&gt;</c> would demand
    /// a durable argument, and <c>IEligibilityRule&lt;IDeclaration&gt;</c> would be reported although an eligibility
    /// rule stores no declaration.
    /// </remarks>
    [Fact]
    public async Task ContravariantTypeArgumentOfAnInterface_IsNotExamined()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] interface ISink<in T> { void Accept(T value); } [Durable] class A { private ISink<ISymbol>? _sink; }" ) );

    /// <remarks>
    /// The counterpart: an interface parameter that is not contravariant is assumed stored, because an interface has
    /// no fields to examine.
    /// </remarks>
    [Fact]
    public async Task InvariantTypeArgumentOfAnInterface_IsExamined()
        => await AssertSingleDiagnosticAsync(
            Code( "[Durable] interface IBox<T> { T Value { get; } } [Durable] class A { private IBox<ISymbol>? _box; }" ),
            "LAMA0870" );

    /// <remarks>
    /// The field of the base is an array of its own type parameter. Substituting the type failed to descend into the
    /// array, so the parameter of the derived type was not recorded as stored and Derived&lt;Compilation&gt; was
    /// accepted.
    /// </remarks>
    [Fact]
    public async Task AnInheritedFieldOfArrayTypeMakesTheParameterStored()
        => await AssertSingleDiagnosticAsync(
            Code( "[Durable] class Base<U> { private readonly U[]? _items; } class Derived<T> : Base<T> { } "
                  + "[Durable] class A { private readonly Derived<SyntaxTree>? _d; }" ),
            "LAMA0870" );

    /// <remarks>
    /// The control: the same shape with a durable type argument is accepted, so the test above measures the argument
    /// and not the array.
    /// </remarks>
    [Fact]
    public async Task AnInheritedFieldOfArrayTypeWithADurableArgument_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class Base<U> { private readonly U[]? _items; } class Derived<T> : Base<T> { } "
                  + "[Durable] class A { private readonly Derived<string>? _d; }" ) );
}
