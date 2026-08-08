// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

/// <summary>
/// Records what Roslyn does with the nullability annotation of a symbol, without involving Metalama.
/// </summary>
/// <remarks>
/// The code model is expected to represent a type the way Roslyn represents it, so that a type the code model builds
/// is indistinguishable from the same type read from source. These tests establish what that representation is, so
/// that the places where the code model has to reproduce it are justified by the behaviour of the compiler rather
/// than by the behaviour of the code model. Nothing here references Metalama: the compilation is created directly and
/// the assertions are on <see cref="ITypeSymbol.NullableAnnotation"/> and on
/// <see cref="SymbolEqualityComparer.IncludeNullability"/>.
/// </remarks>
public sealed class RoslynNullabilityFactsTests
{
    private readonly ITestOutputHelper? _logger;

    public RoslynNullabilityFactsTests( ITestOutputHelper? logger )
    {
        this._logger = logger;
    }

    /// <summary>
    /// Creates a compilation from the given code, and asserts that it has no error, so that no assertion below is
    /// made about a symbol that the compiler considers erroneous.
    /// </summary>
    private static CSharpCompilation CreateCompilation( string code, NullableContextOptions nullableContextOptions = NullableContextOptions.Enable )
    {
        var references = new List<MetadataReference> { MetadataReference.CreateFromFile( typeof(object).Assembly.Location ) };

        // On .NET Core the reference above is System.Private.CoreLib, which does not forward the types that source
        // code binds against, so System.Runtime is added. It does not exist on .NET Framework, where the single
        // reference is sufficient.
        var systemRuntime = Path.Combine( Path.GetDirectoryName( typeof(object).Assembly.Location )!, "System.Runtime.dll" );

        if ( File.Exists( systemRuntime ) )
        {
            references.Add( MetadataReference.CreateFromFile( systemRuntime ) );
        }

        var compilation = CSharpCompilation.Create(
            "test",
            new[] { CSharpSyntaxTree.ParseText( code ) },
            references,
            new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: nullableContextOptions ) );

        var errors = compilation.GetDiagnostics().Where( d => d.Severity == DiagnosticSeverity.Error ).ToList();
        Assert.Empty( errors );

        return compilation;
    }

    private static IFieldSymbol GetField( CSharpCompilation compilation, string typeName, string fieldName )
        => compilation.GetTypeByMetadataName( typeName )!.GetMembers( fieldName ).OfType<IFieldSymbol>().Single();

    /// <summary>
    /// Establishes that Roslyn annotates the symbol of a value type written as <c>T?</c> in source, whatever the
    /// nullable context of the compilation.
    /// </summary>
    /// <remarks>
    /// The annotation is redundant in the sense that <c>Nullable&lt;T&gt;</c> is nullable whether or not it carries
    /// it. It is nonetheless the representation Roslyn produces, and
    /// <see cref="NullableValueTypeConstructedByTheApiIsNotAnnotated"/> shows that it is observable.
    /// </remarks>
    [Theory]
    [InlineData( NullableContextOptions.Enable )]
    [InlineData( NullableContextOptions.Disable )]
    public void NullableValueTypeDeclaredInSourceIsAnnotated( NullableContextOptions nullableContextOptions )
    {
        var compilation = CreateCompilation( "struct S { } class C { public S? Nullable; public S NonNullable; }", nullableContextOptions );

        var nullableType = GetField( compilation, "C", "Nullable" ).Type;
        var nonNullableType = GetField( compilation, "C", "NonNullable" ).Type;

        this._logger?.WriteLine( $"S? is {nullableType}/{nullableType.NullableAnnotation}, S is {nonNullableType}/{nonNullableType.NullableAnnotation}" );

        Assert.Equal( NullableAnnotation.Annotated, nullableType.NullableAnnotation );
        Assert.Equal( SpecialType.System_Nullable_T, nullableType.OriginalDefinition.SpecialType );
    }

    /// <summary>
    /// Establishes that constructing <c>Nullable&lt;T&gt;</c> through the API does not annotate the constructed type,
    /// and that the difference from the type read from source is observable through
    /// <see cref="SymbolEqualityComparer.IncludeNullability"/>.
    /// </summary>
    /// <remarks>
    /// This is why the annotation has to be set explicitly by whatever builds a nullable value type through the API:
    /// the two symbols denote the same type, and a comparison that includes nullability nonetheless tells them apart.
    /// Setting the annotation is what makes them compare equal.
    /// </remarks>
    [Fact]
    public void NullableValueTypeConstructedByTheApiIsNotAnnotated()
    {
        var compilation = CreateCompilation( "struct S { } class C { public S? Nullable; }" );

        var fromSource = GetField( compilation, "C", "Nullable" ).Type;

        var structType = compilation.GetTypeByMetadataName( "S" )!;
        var constructed = compilation.GetSpecialType( SpecialType.System_Nullable_T ).Construct( structType );

        this._logger?.WriteLine( $"from source: {fromSource}/{fromSource.NullableAnnotation}, constructed: {constructed}/{constructed.NullableAnnotation}" );

        Assert.NotEqual( NullableAnnotation.Annotated, constructed.NullableAnnotation );

        // The two symbols denote the same type, so a comparison that ignores nullability finds them equal.
        Assert.True( SymbolEqualityComparer.Default.Equals( fromSource, constructed ) );

        // A comparison that includes nullability does not, which is the observable consequence.
        Assert.False( SymbolEqualityComparer.IncludeNullability.Equals( fromSource, constructed ) );

        // Setting the annotation is what makes the constructed type indistinguishable from the one read from source.
        Assert.True(
            SymbolEqualityComparer.IncludeNullability.Equals( fromSource, constructed.WithNullableAnnotation( NullableAnnotation.Annotated ) ) );
    }

    /// <summary>
    /// Establishes that the declaration of a type parameter constrained to be a reference type is oblivious, whereas
    /// a use of the same parameter is not annotated, and that the difference is observable.
    /// </summary>
    /// <remarks>
    /// This is why the identifier of a type parameter carries no nullability marker. The marker would be applied to
    /// the parameter that the generic context yields, which is the declaration, and annotating the declaration
    /// produces a symbol that Roslyn distinguishes from the one the identifier was built from.
    /// </remarks>
    [Fact]
    public void TypeParameterDeclarationIsObliviousWhereAUseOfItIsNotAnnotated()
    {
        var compilation = CreateCompilation( "class Wrapper<T> { } class C<T> where T : class { public Wrapper<T> Field = null!; }" );

        var declaration = compilation.GetTypeByMetadataName( "C`1" )!.TypeParameters.Single();
        var use = ((INamedTypeSymbol) GetField( compilation, "C`1", "Field" ).Type).TypeArguments.Single();

        this._logger?.WriteLine( $"declaration: {declaration}/{declaration.NullableAnnotation}, use: {use}/{use.NullableAnnotation}" );

        Assert.Equal( NullableAnnotation.None, declaration.NullableAnnotation );
        Assert.Equal( NullableAnnotation.NotAnnotated, use.NullableAnnotation );

        // Annotating the declaration as non-nullable, which is what applying the marker to it amounts to, yields a
        // symbol that a comparison including nullability distinguishes from the declaration itself.
        Assert.False(
            SymbolEqualityComparer.IncludeNullability.Equals(
                declaration,
                declaration.WithNullableAnnotation( NullableAnnotation.NotAnnotated ) ) );
    }

    /// <summary>
    /// Establishes that the declaration of a type parameter constrained to be a value type is already not annotated,
    /// so that applying the non-nullable marker to it would be a no-operation rather than a change.
    /// </summary>
    /// <remarks>
    /// The constraints therefore do not agree on what the declaration of a parameter carries, which is why the
    /// identifier cannot treat them uniformly by applying the marker, and does so by not applying it.
    /// </remarks>
    [Theory]
    [InlineData( "struct", NullableAnnotation.NotAnnotated )]
    [InlineData( "unmanaged", NullableAnnotation.NotAnnotated )]
    [InlineData( "class", NullableAnnotation.None )]
    [InlineData( "notnull", NullableAnnotation.None )]
    public void TypeParameterDeclarationAnnotationDependsOnTheConstraint( string constraint, NullableAnnotation expectedAnnotation )
    {
        var compilation = CreateCompilation( $"class C<T> where T : {constraint} {{ }}" );

        var declaration = compilation.GetTypeByMetadataName( "C`1" )!.TypeParameters.Single();

        this._logger?.WriteLine( $"where T : {constraint} -> {declaration.NullableAnnotation}" );

        Assert.Equal( expectedAnnotation, declaration.NullableAnnotation );
    }
}
