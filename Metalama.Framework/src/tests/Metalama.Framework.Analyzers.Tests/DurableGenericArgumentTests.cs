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
}
