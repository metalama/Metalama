// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.SerializableIds;

/// <summary>
/// Establishes which of the two declaration shapes reported in
/// https://github.com/metalama/Metalama/issues/2051 has no <see cref="SerializableDeclarationId"/>.
/// </summary>
/// <remarks>
/// The report named two refused symbols: a method of a type emitted by the ASP.NET Core OpenAPI XML-comment source
/// generator, and a user-written method whose signature contains an <c>in</c> parameter and a <c>ref</c> parameter of
/// a generic type. A declaration identifier is a documentation-comment identifier, which names a type by its
/// namespace and its name only. Two file-local types can share both, so the provider refuses a declaration of a
/// file-local type rather than produce an identifier that resolves to the wrong one. This is the gap recorded in
/// #662. Parameter reference kinds, on the other hand, are represented: both <c>in</c> and <c>ref</c> serialize to
/// the <c>@</c> suffix, and the identifier round-trips.
/// </remarks>
public sealed class FileLocalTypeIdTests : UnitTestClass
{
    public FileLocalTypeIdTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Returns every declared member symbol of the given code, in declaration order.
    /// </summary>
    private IEnumerable<ISymbol> GetDeclaredSymbols( TestContext testContext, string code, out ICompilation compilation )
    {
        compilation = testContext.CreateCompilation( code );
        var roslynCompilation = compilation.GetRoslynCompilation();
        var tree = roslynCompilation.SyntaxTrees.Single();
        var semanticModel = roslynCompilation.GetSemanticModel( tree );

        return tree.GetRoot()
            .DescendantNodes()
            .OfType<MemberDeclarationSyntax>()
            .Select( n => semanticModel.GetDeclaredSymbol( n ) )
            .Where( s => s != null )
            .Select( s => s! )
            .ToList();
    }

    [Fact]
    public void FileLocalTypeAndItsMembersHaveNoId()
    {
        using var testContext = this.CreateTestContext();

        const string code = """
                            namespace N;

                            file class F
                            {
                                public void M( int i ) { }
                            }
                            """;

        var symbols = this.GetDeclaredSymbols( testContext, code, out _ )
            .Where( s => s.Kind is SymbolKind.NamedType or SymbolKind.Method )
            .ToList();

        Assert.Equal( 2, symbols.Count );

        foreach ( var symbol in symbols )
        {
            this.TestOutput.WriteLine( symbol.ToDisplayString() );
            Assert.False( symbol.TryGetSerializableId( out _ ) );
        }
    }

    [Fact]
    public void InAndRefParametersHaveAnId()
    {
        using var testContext = this.CreateTestContext();

        const string code = """
                            namespace N;

                            class C
                            {
                                public void M<T>( in C c, ref T t ) { }
                            }
                            """;

        var method = this.GetDeclaredSymbols( testContext, code, out var compilation )
            .Single( s => s.Kind == SymbolKind.Method );

        Assert.True( method.TryGetSerializableId( out var id ) );
        Assert.Equal( "M:N.C.M``1(N.C@,``0@)", id.Id );
        Assert.Same( method, id.ResolveToSymbolOrNull( compilation.GetCompilationContext() ) );
    }
}
