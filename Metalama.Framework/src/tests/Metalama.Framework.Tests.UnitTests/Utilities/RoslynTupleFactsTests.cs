// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

/// <summary>
/// Records what Roslyn does with the names of the elements of a tuple, without involving Metalama.
/// </summary>
/// <remarks>
/// The code model has two comparers, one that takes the nullability of a type into account and one that does not, and
/// it has no comparison that is aware of the names of the elements of a tuple. Deciding whether that is a gap requires
/// knowing whether Roslyn treats the names as part of the identity of a symbol, which is what these tests establish.
/// Nothing here references Metalama.
/// </remarks>
public sealed class RoslynTupleFactsTests
{
    private readonly ITestOutputHelper? _logger;

    public RoslynTupleFactsTests( ITestOutputHelper? logger )
    {
        this._logger = logger;
    }

    private static CSharpCompilation CreateCompilation( string code )
    {
        var references = new List<MetadataReference> { MetadataReference.CreateFromFile( typeof(object).Assembly.Location ) };

        var systemRuntime = Path.Combine( Path.GetDirectoryName( typeof(object).Assembly.Location )!, "System.Runtime.dll" );

        if ( File.Exists( systemRuntime ) )
        {
            references.Add( MetadataReference.CreateFromFile( systemRuntime ) );
        }

        var compilation = CSharpCompilation.Create(
            "test",
            new[] { CSharpSyntaxTree.ParseText( code ) },
            references,
            new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable ) );

        Assert.Empty( compilation.GetDiagnostics().Where( d => d.Severity == DiagnosticSeverity.Error ) );

        return compilation;
    }

    private static INamedTypeSymbol GetFieldType( CSharpCompilation compilation, string fieldName )
        => (INamedTypeSymbol) compilation.GetTypeByMetadataName( "C" )!.GetMembers( fieldName ).OfType<IFieldSymbol>().Single().Type;

    private const string _code = "class C { public (int Count, string Name) Named; public (int, string) Unnamed; }";

    /// <summary>
    /// Establishes that the symbol of a tuple read from source carries the names of its elements, and that a tuple
    /// whose elements are not named carries the default names instead.
    /// </summary>
    [Fact]
    public void TheSymbolOfATupleCarriesTheNamesOfItsElements()
    {
        var compilation = CreateCompilation( _code );

        var named = GetFieldType( compilation, "Named" );
        var unnamed = GetFieldType( compilation, "Unnamed" );

        this._logger?.WriteLine( $"named: {named}, elements {string.Join( ",", named.TupleElements.Select( e => e.Name ) )}" );
        this._logger?.WriteLine( $"unnamed: {unnamed}, elements {string.Join( ",", unnamed.TupleElements.Select( e => e.Name ) )}" );

        Assert.True( named.IsTupleType );
        Assert.Equal( new[] { "Count", "Name" }, named.TupleElements.Select( e => e.Name ) );
        Assert.Equal( new[] { "Item1", "Item2" }, unnamed.TupleElements.Select( e => e.Name ) );
    }

    /// <summary>
    /// Establishes that both comparers of Roslyn treat the names of the elements of a tuple as part of the identity
    /// of the symbol, and that the hash code nonetheless ignores them.
    /// </summary>
    /// <remarks>
    /// This is what decides whether the code model can record the names on the symbol without changing the meaning of
    /// its existing comparisons. Since even the comparer that ignores nullability distinguishes them, and since
    /// <see cref="SymbolEqualityComparer"/> offers no option to ignore them, recording the names on the symbol would
    /// change the result of every comparison the code model makes, including the one a caller chose in order to be
    /// lenient. A comparison that ignores the names would then have to normalize the tuple itself.
    /// </remarks>
    [Fact]
    public void TheComparersOfRoslynDistinguishTheNamesOfTheElementsOfATuple()
    {
        var compilation = CreateCompilation( _code );

        var named = GetFieldType( compilation, "Named" );
        var unnamed = GetFieldType( compilation, "Unnamed" );

        this._logger?.WriteLine(
            $"Default={SymbolEqualityComparer.Default.Equals( named, unnamed )}, "
            + $"IncludeNullability={SymbolEqualityComparer.IncludeNullability.Equals( named, unnamed )}, "
            + $"sameHashCode={SymbolEqualityComparer.Default.GetHashCode( named ) == SymbolEqualityComparer.Default.GetHashCode( unnamed )}" );

        Assert.False( SymbolEqualityComparer.Default.Equals( named, unnamed ) );
        Assert.False( SymbolEqualityComparer.IncludeNullability.Equals( named, unnamed ) );

        // The names are excluded from the hash code, so the two are in the same bucket and separated by the equality,
        // which is consistent and means a dictionary keyed by symbol does not need the names to hash well.
        Assert.Equal( SymbolEqualityComparer.Default.GetHashCode( named ), SymbolEqualityComparer.Default.GetHashCode( unnamed ) );

        // The underlying type is what the two have in common, so it is what a comparison that ignores the names would
        // have to compare.
        Assert.True( SymbolEqualityComparer.Default.Equals( named.TupleUnderlyingType, unnamed.TupleUnderlyingType ?? unnamed ) );
    }

    /// <summary>
    /// Establishes that a tuple built through the API with element names is equal to the tuple read from source that
    /// has the same names, and carries them.
    /// </summary>
    /// <remarks>
    /// This is the operation the code model would use to record the names on the symbol.
    /// </remarks>
    [Fact]
    public void ATupleBuiltWithElementNamesCarriesThem()
    {
        var compilation = CreateCompilation( _code );

        var named = GetFieldType( compilation, "Named" );
        var unnamed = GetFieldType( compilation, "Unnamed" );

        var built = compilation.CreateTupleTypeSymbol(
            unnamed.TupleUnderlyingType ?? unnamed,
            ImmutableArray.Create<string?>( "Count", "Name" ) );

        this._logger?.WriteLine( $"built: {built}, elements {string.Join( ",", built.TupleElements.Select( e => e.Name ) )}" );

        Assert.Equal( new[] { "Count", "Name" }, built.TupleElements.Select( e => e.Name ) );
        Assert.True( SymbolEqualityComparer.Default.Equals( named, built ) );
    }

    /// <summary>
    /// Establishes that the names of the elements of a tuple do not take part in the conversion between two tuples,
    /// which is an identity conversion whatever the elements are called.
    /// </summary>
    /// <remarks>
    /// Whatever comparison the code model gains, the logic of conversion has to keep ignoring the names.
    /// </remarks>
    [Fact]
    public void TheNamesOfTheElementsOfATupleDoNotTakePartInConversion()
    {
        var compilation = CreateCompilation( _code );

        var named = GetFieldType( compilation, "Named" );
        var unnamed = GetFieldType( compilation, "Unnamed" );

        var conversion = compilation.ClassifyConversion( named, unnamed );

        this._logger?.WriteLine( $"conversion from {named} to {unnamed}: identity={conversion.IsIdentity}, implicit={conversion.IsImplicit}" );

        Assert.True( conversion.IsIdentity );
    }
}
