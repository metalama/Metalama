// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Comparers;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using SymbolEqualityComparer = Microsoft.CodeAnalysis.SymbolEqualityComparer;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Verifies that the comparison and the conversion of the code model answer exactly what Roslyn answers for the same
/// types.
/// </summary>
/// <remarks>
/// <para>
/// Roslyn is the authority: the equality of two types is the equality of their symbols, and the convertibility of one
/// to another is what <c>ClassifyConversion</c> says. The code model reimplements both, so that it can answer for a
/// type an aspect introduced, which has no symbol. The reimplementation has to agree with Roslyn wherever a symbol
/// exists, otherwise an aspect gets a different answer depending on where the type came from.
/// </para>
/// <para>
/// Every pair of a corpus of types read from source is checked, and the conversions are checked twice: once as the
/// code model answers them by default, which is by delegating to Roslyn when both types have a symbol, and once with
/// that delegation suppressed, which is the path taken for an introduced type. The two must agree with each other and
/// with Roslyn.
/// </para>
/// <para>
/// The equality of two types has no such switch, because the code model always answers it structurally.
/// </para>
/// </remarks>
public sealed class ComparerAgreesWithRoslynTests : UnitTestClass
{
    public ComparerAgreesWithRoslynTests( ITestOutputHelper? logger ) : base( logger ) { }

    private const string _code = """
                                 using System.Collections.Generic;

                                 class A { }

                                 interface I { }

                                 class B : A, I
                                 {
                                     public static implicit operator int( B b ) => 42;
                                 }

                                 struct S { }

                                 class Corpus
                                 {
                                     public object Object = null!;
                                     public object? NullableObject;
                                     public string String = null!;
                                     public string? NullableString;
                                     public int Int;
                                     public int? NullableInt;
                                     public S Struct;
                                     public S? NullableStruct;
                                     public (int Count, string Name) NamedTuple;
                                     public (int, string) UnnamedTuple;
                                     public (int Other, string Different) DifferentlyNamedTuple;
                                     public List<(int Count, string Name)> ListOfNamedTuple = null!;
                                     public List<(int Other, string Different)> ListOfDifferentlyNamedTuple = null!;
                                     public (int Count, string Name)[] ArrayOfNamedTuple = null!;
                                     public (int Other, string Different)[] ArrayOfDifferentlyNamedTuple = null!;
                                     public A A = null!;
                                     public B B = null!;
                                     public I I = null!;
                                     public int[] IntArray = null!;
                                     public List<string> ListOfString = null!;
                                     public List<string?> ListOfNullableString = null!;
                                 }
                                 """;

    /// <summary>
    /// Checks every pair of the corpus and reports every disagreement at once, rather than stopping at the first, so
    /// that the extent of the difference is visible.
    /// </summary>
    [Fact]
    public void TheCodeModelAnswersWhatRoslynAnswers()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var corpus = compilation.Types.OfName( "Corpus" ).Single();

        var types = corpus.Fields
            .Where( f => !f.IsImplicitlyDeclared )
            .Select( f => (Name: f.Name, Type: f.Type) )
            .ToList();

        var defaultComparer = (DeclarationEqualityComparer) compilation.CompilationContext.Comparers.Default;
        var nullabilityComparer = (DeclarationEqualityComparer) compilation.CompilationContext.Comparers.IncludeNullability;
        var roslynCompilation = (CSharpCompilation) compilation.RoslynCompilation;

        var mismatches = new List<string>();

        void Check( string what, string pair, bool roslyn, bool metalama )
        {
            if ( roslyn != metalama )
            {
                mismatches.Add( $"{pair} | {what} | roslyn={roslyn} metalama={metalama}" );
            }
        }

        foreach ( var (leftName, left) in types )
        {
            foreach ( var (rightName, right) in types )
            {
                var leftSymbol = left.GetSymbol()!;
                var rightSymbol = right.GetSymbol()!;
                var pair = $"{leftName} -> {rightName}";

                var conversion = roslynCompilation.ClassifyConversion( leftSymbol, rightSymbol );

                Check(
                    "Equals/Default",
                    pair,
                    SymbolEqualityComparer.Default.Equals( leftSymbol, rightSymbol ),
                    defaultComparer.Equals( left, right ) );

                Check(
                    "Equals/IncludeNullability",
                    pair,
                    SymbolEqualityComparer.IncludeNullability.Equals( leftSymbol, rightSymbol ),
                    nullabilityComparer.Equals( left, right ) );

                // A hash code cannot be compared across two implementations, but the contract that two equal types
                // hash equally has to hold within each of them, and a comparer that answers equality wrongly usually
                // breaks it. The hash is therefore checked against the equality of the same comparer.
                foreach ( var (comparerName, comparer) in new[] { ("Default", defaultComparer), ("IncludeNullability", nullabilityComparer) } )
                {
                    if ( comparer.Equals( left, right ) )
                    {
                        Check(
                            $"GetHashCode/{comparerName}",
                            pair,
                            true,
                            comparer.GetHashCode( left ) == comparer.GetHashCode( right ) );
                    }
                }

                foreach ( var bypassSymbols in new[] { false, true } )
                {
                    // The conversion is classified by the language and does not depend on the nullability of the
                    // types, so both comparers have to give the answer that Roslyn gives.
                    foreach ( var (comparerName, comparer) in new[] { ("Default", defaultComparer), ("IncludeNullability", nullabilityComparer) } )
                    {
                        Check(
                            $"Identical/{comparerName}/bypassSymbols={bypassSymbols}",
                            pair,
                            conversion.IsIdentity,
                            comparer.IsConvertibleTo( left, right, ConversionKind.Identical, bypassSymbols ) );

                        Check(
                            $"Implicit/{comparerName}/bypassSymbols={bypassSymbols}",
                            pair,
                            conversion.IsImplicit,
                            comparer.IsConvertibleTo( left, right, ConversionKind.Implicit, bypassSymbols ) );
                    }
                }
            }
        }

        foreach ( var mismatch in mismatches.Take( 60 ) )
        {
            this.TestOutput.WriteLine( mismatch );
        }

        this.TestOutput.WriteLine( $"total mismatches: {mismatches.Count} over {types.Count * types.Count} pairs" );

        Assert.Empty( mismatches );
    }
}
