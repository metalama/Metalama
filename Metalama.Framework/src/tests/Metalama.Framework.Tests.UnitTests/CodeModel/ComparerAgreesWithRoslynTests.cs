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
#if ROSLYN_5_11_0_OR_GREATER && NET7_0_OR_GREATER
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
#endif

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

        this.CheckEveryPair( compilation, "Corpus" );
    }

    /// <summary>
    /// Checks every pair of the types of the fields of the type named <paramref name="corpusTypeName"/>, and reports
    /// every disagreement at once rather than stopping at the first.
    /// </summary>
    private void CheckEveryPair( CompilationModel compilation, string corpusTypeName )
    {
        var corpus = compilation.Types.OfName( corpusTypeName ).Single();

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

#if ROSLYN_5_11_0_OR_GREATER && NET7_0_OR_GREATER

    // The union is a C# 15 feature, so only the latest Roslyn variant parses it. The case is further restricted to
    // .NET 7 and later, because the compiler emits CompilerFeatureRequiredAttribute on the members of a union and
    // .NET Framework does not declare that type.

    /// <summary>
    /// The declarations that the compiler requires of a union. No target framework declares them yet, and the
    /// compiler reports CS0656 when it cannot find them, so the compilation of the union corpus declares them.
    /// </summary>
    private const string _unionSupportCode = """
                                             using System;

                                             namespace System.Runtime.CompilerServices
                                             {
                                                 [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct )]
                                                 public sealed class UnionAttribute : Attribute;

                                                 public interface IUnion;
                                             }
                                             """;

    /// <summary>
    /// The union corpus. Two unions have the same case list, so that a conversion between two unions is checked as
    /// well as a conversion from a case type. One record derives from a case type and one record is a case of
    /// neither union, so that the corpus separates a case type from a type that merely converts to one and from a
    /// type that does not convert at all.
    /// </summary>
    private const string _unionCode = """
                                      union Pet( Cat, Dog );

                                      union Animal( Cat, Dog );

                                      record Cat( string Name );

                                      record Siamese( string Name ) : Cat( Name );

                                      record Dog( string Name );

                                      record Fish( string Name );

                                      class UnionCorpus
                                      {
                                          public Pet Pet;
                                          public Animal Animal;
                                          public Cat Cat = null!;
                                          public Siamese Siamese = null!;
                                          public Dog Dog = null!;
                                          public Fish Fish = null!;
                                          public object Object = null!;
                                      }
                                      """;

    /// <summary>
    /// The union case that issue #1945 asks for. The conversion from a case type to its union is granted by the
    /// language and is not an <c>op_Implicit</c> method, so the reimplementation that answers for a type with no
    /// symbol has to know it.
    /// </summary>
    [Fact]
    public void TheCodeModelAnswersWhatRoslynAnswersForAUnion()
    {
        using var testContext = this.CreateTestContext();

        var parseOptions = SupportedCSharpVersions.DefaultParseOptions;

        var roslynCompilation = testContext.CreateEmptyCSharpCompilation( null )
            .AddSyntaxTrees(
                CSharpSyntaxTree.ParseText( _unionSupportCode, parseOptions, "support.cs" ),
                CSharpSyntaxTree.ParseText( _unionCode, parseOptions, "unions.cs" ) );

        Assert.Empty( roslynCompilation.GetDiagnostics().Where( d => d.Severity == DiagnosticSeverity.Error ) );

        var compilation = testContext.CreateCompilationModel( roslynCompilation );

        this.CheckEveryPair( compilation, "UnionCorpus" );

        // The pairwise check proves that the two implementations agree, which a pair that neither of them converts
        // also satisfies. These assertions pin what the answer is, so that the case cannot pass because the
        // conversion disappeared from both sides.
        //
        // The derived case type is pinned explicitly, because the rule is not obvious and was read the other way
        // round during review. Roslyn classifies the conversion from Siamese to Pet as implicit, so the source of
        // the conversion is any type that converts to a case type and not the case type alone. The three lines
        // below are the ones that would fail if the reimplementation required an identity conversion instead.
        var pet = compilation.Types.OfName( "Pet" ).Single();
        var cat = compilation.Types.OfName( "Cat" ).Single();
        var siamese = compilation.Types.OfName( "Siamese" ).Single();
        var fish = compilation.Types.OfName( "Fish" ).Single();
        var comparer = (DeclarationEqualityComparer) compilation.CompilationContext.Comparers.Default;

        var roslynSaysSiameseConvertsToPet = ((CSharpCompilation) compilation.RoslynCompilation)
            .ClassifyConversion( siamese.GetSymbol()!, pet.GetSymbol()! )
            .IsImplicit;

        Assert.True( roslynSaysSiameseConvertsToPet, "Roslyn should grant an implicit conversion from 'Siamese' to 'Pet'." );

        foreach ( var bypassSymbols in new[] { false, true } )
        {
            Assert.True(
                comparer.IsConvertibleTo( cat, pet, ConversionKind.Implicit, bypassSymbols ),
                $"'Cat' should be implicitly convertible to 'Pet' with bypassSymbols={bypassSymbols}." );

            Assert.True(
                comparer.IsConvertibleTo( siamese, pet, ConversionKind.Implicit, bypassSymbols ),
                $"'Siamese' should be implicitly convertible to 'Pet' with bypassSymbols={bypassSymbols}." );

            Assert.False(
                comparer.IsConvertibleTo( fish, pet, ConversionKind.Implicit, bypassSymbols ),
                $"'Fish' should not be implicitly convertible to 'Pet' with bypassSymbols={bypassSymbols}." );
        }
    }

#endif
}
