// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Analyzers.Durability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating.Immutability;

/// <summary>
/// Asserts the four points on which the durability and immutability contracts deliberately disagree.
/// </summary>
/// <remarks>
/// <para>
/// <c>Durable</c> asks whether an object may be held across compilations. <c>ImmutableType</c> asks whether it can
/// change. Those are different questions, and on four kinds of type they have opposite answers. Two warnings with
/// opposite verdicts on one field read as a bug unless something says otherwise, so each disagreement is asserted
/// here and explained in the header of both tables.
/// </para>
/// <para>
/// A failure here means one of two things: either a table was edited without the other being considered, or a
/// disagreement was resolved on purpose. In the second case the fix is to delete the test and the paragraph in the
/// table that it refers to, together.
/// </para>
/// </remarks>
public sealed class DurabilityImmutabilityDivergenceTests : ImmutabilityTestBase
{
    /// <summary>
    /// Runs the durability analyzer and the immutability rules over one type that carries both contracts, and returns
    /// whether each of them reported a diagnostic.
    /// </summary>
    private async Task<(bool Durability, bool Immutability)> GetVerdictsAsync( string memberDeclaration )
    {
        var code = $$"""
                     using System;
                     using System.Collections.Generic;
                     using Metalama.Framework.Utilities;
                     using Microsoft.CodeAnalysis;
                     using Microsoft.CodeAnalysis.CSharp;
                     using Microsoft.CodeAnalysis.CSharp.Syntax;
                     using Microsoft.CodeAnalysis.Operations;
                     using Microsoft.CodeAnalysis.Text;

                     [Durable]
                     [ImmutableType]
                     class C
                     {
                         {{memberDeclaration}}
                     }
                     """;

        using var testContext = this.CreateTestContext();

        var durabilityDiagnostics = await this.CreateCompilation( testContext, code )
            .WithAnalyzers( ImmutableArray.Create<DiagnosticAnalyzer>( new DurableContractAnalyzer() ) )
            .GetAnalyzerDiagnosticsAsync( testContext.CancellationToken );

        var immutabilityDiagnostics = await this.GetDiagnosticsAsync( code );

        return (
            durabilityDiagnostics.Any( d => d.Id.StartsWith( "LAMA087", StringComparison.Ordinal ) ),
            immutabilityDiagnostics.Any( d => d.Id.StartsWith( "LAMA088", StringComparison.Ordinal ) ));
    }

    /// <remarks>
    /// A delegate cannot be retargeted, so it is immutable. It holds its target and everything its closure captured,
    /// so it is not durable.
    /// </remarks>
    [Fact]
    public async Task ADelegate_IsNotDurableButIsImmutable()
    {
        var (durability, immutability) = await GetVerdictsAsync( "private readonly Func<int, int> _f = x => x;" );

        Assert.True( durability, "A delegate should not be durable." );
        Assert.False( immutability, "A delegate should be immutable." );
    }

    /// <remarks>
    /// An array of a durable element type reaches nothing that is bound to a compilation, so it is durable. Every one
    /// of its elements can be replaced, so it is never immutable.
    /// </remarks>
    [Fact]
    public async Task AnArray_IsDurableButIsNotImmutable()
    {
        var (durability, immutability) = await GetVerdictsAsync( "private readonly int[] _a = new int[0];" );

        Assert.False( durability, "An array of a durable element type should be durable." );
        Assert.True( immutability, "An array should not be immutable." );
    }

    /// <remarks>
    /// A weak reference does not keep its target alive, which is the whole reason it is durable whatever its type
    /// argument is. It can be retargeted, so it is mutable.
    /// </remarks>
    [Fact]
    public async Task AWeakReference_IsDurableButIsNotImmutable()
    {
        // The name satisfies LAMA0875, which is a durability rule of its own and would otherwise be the diagnostic
        // this test measured.
        var (durability, immutability) = await GetVerdictsAsync(
            "private readonly WeakReference<Compilation>? _compilationDangerous;" );

        Assert.False( durability, "A weak reference should be durable whatever its type argument is." );
        Assert.True( immutability, "A weak reference should not be immutable." );
    }

    /// <remarks>
    /// <para>
    /// The sharpest case, and the one an earlier version of the table got wrong by importing the durability reasons
    /// wholesale. "A compilation is rebuilt on every edit" and "a syntax node belongs to one syntax tree" are
    /// statements about lifetime, not about mutability. Roslyn's public API is immutable by construction: syntax
    /// trees are persistent, every <c>With</c> method returns a new instance, and a symbol never changes.
    /// </para>
    /// <para>
    /// So these are the clearest example of the two contracts disagreeing, not of them agreeing.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( "private readonly SyntaxTree? _tree;" )]
    [InlineData( "private readonly SyntaxNode? _node;" )]
    [InlineData( "private readonly Compilation? _compilation;" )]
    [InlineData( "private readonly ISymbol? _symbol;" )]
    [InlineData( "private readonly Location? _location;" )]
    public async Task ARoslynObject_IsNotDurableButIsImmutable( string memberDeclaration )
    {
        var (durability, immutability) = await GetVerdictsAsync( memberDeclaration );

        Assert.True( durability, "A Roslyn object should not be durable." );
        Assert.False( immutability, "A Roslyn object should be immutable." );
    }

    /// <remarks>
    /// The Roslyn entries of the immutability table also cover the types that derive from them and the interfaces that
    /// extend them. Without that, the entry of <c>SyntaxNode</c> would not cover <c>TypeSyntax</c>, and the entry
    /// would have almost no effect, because a field is rarely typed as <c>SyntaxNode</c> itself.
    /// </remarks>
    [Theory]
    [InlineData( "private readonly TypeSyntax? _type;" )]
    [InlineData( "private readonly MethodDeclarationSyntax? _method;" )]
    [InlineData( "private readonly CSharpSyntaxNode? _csharpNode;" )]
    [InlineData( "private readonly IMethodSymbol? _methodSymbol;" )]
    [InlineData( "private readonly ITypeSymbol? _typeSymbol;" )]
    [InlineData( "private readonly INamedTypeSymbol? _namedTypeSymbol;" )]
    [InlineData( "private readonly CSharpCompilation? _csharpCompilation;" )]
    [InlineData( "private readonly SemanticModel? _semanticModel;" )]
    [InlineData( "private readonly SourceText? _text;" )]
    public async Task ARoslynObjectOfADerivedType_IsNotDurableButIsImmutable( string memberDeclaration )
    {
        var (durability, immutability) = await GetVerdictsAsync( memberDeclaration );

        Assert.True( durability, "A Roslyn object should not be durable." );
        Assert.False( immutability, "A Roslyn object should be immutable." );
    }

    /// <remarks>
    /// These types of the public Roslyn API are immutable by construction. They are bound to a compilation, to a syntax
    /// tree or to an operation tree, so they are not durable.
    /// </remarks>
    [Theory]
    [InlineData( "private readonly Diagnostic? _diagnostic;" )]
    [InlineData( "private readonly SyntaxToken _token;" )]
    [InlineData( "private readonly SyntaxTrivia _trivia;" )]
    [InlineData( "private readonly SyntaxNodeOrToken _nodeOrToken;" )]
    [InlineData( "private readonly SyntaxTokenList _tokens;" )]
    [InlineData( "private readonly SyntaxTriviaList _triviaList;" )]
    [InlineData( "private readonly SyntaxList<StatementSyntax> _statements;" )]
    [InlineData( "private readonly SeparatedSyntaxList<ParameterSyntax> _parameters;" )]
    [InlineData( "private readonly SyntaxNodeOrTokenList _nodesOrTokens;" )]
    [InlineData( "private readonly AttributeData? _attribute;" )]
    [InlineData( "private readonly TypedConstant _constant;" )]
    [InlineData( "private readonly IOperation? _operation;" )]
    [InlineData( "private readonly IInvocationOperation? _invocation;" )]
    public async Task AnotherRoslynObject_IsNotDurableButIsImmutable( string memberDeclaration )
    {
        var (durability, immutability) = await GetVerdictsAsync( memberDeclaration );

        Assert.True( durability, "A Roslyn object should not be durable." );
        Assert.False( immutability, "A Roslyn object should be immutable." );
    }

    /// <remarks>
    /// These types of the public Roslyn API are immutable by construction and are not bound to a compilation.
    /// </remarks>
    [Theory]
    [InlineData( "private readonly DiagnosticDescriptor? _descriptor;" )]
    [InlineData( "private readonly LocalizableString? _localizableString;" )]
    [InlineData( "private readonly LocalizableResourceString? _localizableResourceString;" )]
    [InlineData( "private readonly TextSpan _span;" )]
    [InlineData( "private readonly LinePosition _linePosition;" )]
    [InlineData( "private readonly LinePositionSpan _linePositionSpan;" )]
    [InlineData( "private readonly FileLinePositionSpan _fileLinePositionSpan;" )]
    [InlineData( "private readonly TextChange _change;" )]
    [InlineData( "private readonly ParseOptions? _parseOptions;" )]
    [InlineData( "private readonly CSharpParseOptions? _csharpParseOptions;" )]
    [InlineData( "private readonly CompilationOptions? _compilationOptions;" )]
    [InlineData( "private readonly CSharpCompilationOptions? _csharpCompilationOptions;" )]
    [InlineData( "private readonly SymbolDisplayFormat? _format;" )]
    public async Task AnUnboundRoslynObject_IsImmutable( string memberDeclaration )
    {
        var (_, immutability) = await GetVerdictsAsync( memberDeclaration );

        Assert.False( immutability, "A Roslyn object should be immutable." );
    }

    /// <remarks>
    /// The other control: a type both contracts accept. Without it, an analyzer that reported everything would pass
    /// every assertion above.
    /// </remarks>
    [Fact]
    public async Task AString_IsBothDurableAndImmutable()
    {
        var (durability, immutability) = await GetVerdictsAsync( "private readonly string _name = \"\";" );

        Assert.False( durability, "A string should be durable." );
        Assert.False( immutability, "A string should be immutable." );
    }
}
