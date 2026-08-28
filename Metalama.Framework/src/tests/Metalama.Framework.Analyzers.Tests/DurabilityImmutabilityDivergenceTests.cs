// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.ComponentModel;
using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Asserts the four points on which the two contracts of this assembly deliberately disagree.
/// </summary>
/// <remarks>
/// <para>
/// <c>Durable</c> asks whether an object may be held across compilations. <c>ImmutableObject</c> asks whether it can
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
public sealed class DurabilityImmutabilityDivergenceTests
{
    private static readonly MetadataReference[] _references =
    [
        MetadataReference.CreateFromFile( typeof(object).Assembly.Location ),
        MetadataReference.CreateFromFile(
            Path.Combine( Path.GetDirectoryName( typeof(object).Assembly.Location )!, "netstandard.dll" ) ),
        MetadataReference.CreateFromFile(
            Path.Combine( Path.GetDirectoryName( typeof(object).Assembly.Location )!, "System.Runtime.dll" ) ),
        MetadataReference.CreateFromFile(
            Path.Combine( Path.GetDirectoryName( typeof(object).Assembly.Location )!, "System.Collections.dll" ) ),
        MetadataReference.CreateFromFile( typeof(ImmutableObjectAttribute).Assembly.Location ),
        MetadataReference.CreateFromFile( typeof(SyntaxNode).Assembly.Location ),
        MetadataReference.CreateFromFile( typeof(DurableAttribute).Assembly.Location )
    ];

    /// <summary>
    /// Runs both analyzers over one type that carries both contracts, and returns the identifiers each reported.
    /// </summary>
    private static async Task<(bool Durability, bool Immutability)> GetVerdictsAsync( string memberDeclaration )
    {
        var code = $$"""
                     using System;
                     using System.Collections.Generic;
                     using System.ComponentModel;
                     using Metalama.Framework.Utilities;
                     using Microsoft.CodeAnalysis;

                     [Durable]
                     [ImmutableObject(true)]
                     class C
                     {
                         {{memberDeclaration}}
                     }
                     """;

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText( code, new CSharpParseOptions( LanguageVersion.CSharp12 ) )],
            _references,
            new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );

        var diagnostics = await compilation
            .WithAnalyzers(
                ImmutableArray.Create<DiagnosticAnalyzer>(
                    new DurableContractAnalyzer(),
                    new ImmutableContractAnalyzer() ) )
            .GetAnalyzerDiagnosticsAsync();

        return (
            diagnostics.Any( d => d.Id.StartsWith( "LAMA087", StringComparison.Ordinal ) ),
            diagnostics.Any( d => d.Id.StartsWith( "LAMA088", StringComparison.Ordinal ) ));
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
    /// The control. Where the two contracts agree, they must both fire, or the test above would pass for a type that
    /// simply escapes one of the two analyzers.
    /// </remarks>
    [Fact]
    public async Task ASyntaxTree_IsNeitherDurableNorImmutable()
    {
        var (durability, immutability) = await GetVerdictsAsync( "private readonly SyntaxTree? _tree;" );

        Assert.True( durability, "A SyntaxTree should not be durable." );
        Assert.True( immutability, "A SyntaxTree should not be immutable." );
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
