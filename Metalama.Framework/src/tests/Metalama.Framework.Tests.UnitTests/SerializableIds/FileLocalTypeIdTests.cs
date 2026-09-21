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
using System;
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
/// a generic type. The first was refused for its containing type, which was file-local, and the second was not
/// refused at all: both <c>in</c> and <c>ref</c> serialize to the <c>@</c> suffix, and the identifier round-trips.
/// A declaration of a file-local type now has an identifier as well, because the identifier carries the metadata name
/// of that type as a discriminator, which closes the gap recorded in #662.
/// </remarks>
public sealed class FileLocalTypeIdTests : UnitTestClass
{
    public FileLocalTypeIdTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Returns every declared member symbol of the given code, in declaration order.
    /// </summary>
    private static IEnumerable<ISymbol> GetDeclaredSymbols( TestContext testContext, string code, out ICompilation compilation )
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
    public void FileLocalTypeAndItsMembersHaveAnId()
    {
        using var testContext = this.CreateTestContext();

        const string code = """
                            namespace N;

                            file class F
                            {
                                public void M( int i ) { }
                            }
                            """;

        var symbols = GetDeclaredSymbols( testContext, code, out var compilation )
            .Where( s => s.Kind is SymbolKind.NamedType or SymbolKind.Method )
            .ToList();

        Assert.Equal( 2, symbols.Count );

        foreach ( var symbol in symbols )
        {
            this.TestOutput.WriteLine( symbol.ToDisplayString() );

            Assert.True( symbol.TryGetSerializableId( out var id ) );
            this.TestOutput.WriteLine( id.Id );

            // The discriminator is the metadata name of the file-local type, which the compiler builds from the name
            // of the declaring file and the checksum of its path.
            Assert.Contains( ";File=<test>F", id.Id, StringComparison.Ordinal );
            Assert.EndsWith( "__F", id.Id, StringComparison.Ordinal );

            // The comparison ignores the nullable annotation, because resolution normalizes it on a named type and
            // therefore returns a symbol that differs from the declared one in that respect alone.
            Assert.Equal(
                symbol,
                id.ResolveToSymbolOrNull( compilation.GetCompilationContext() ),
                SymbolEqualityComparer.Default );
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

        var method = GetDeclaredSymbols( testContext, code, out var compilation )
            .Single( s => s.Kind == SymbolKind.Method );

        Assert.True( method.TryGetSerializableId( out var id ) );
        Assert.Equal( "M:N.C.M``1(N.C@,``0@)", id.Id );
        Assert.Same( method, id.ResolveToSymbolOrNull( compilation.GetCompilationContext() ) );
    }
}
