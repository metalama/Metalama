// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using SymbolEqualityComparer = Microsoft.CodeAnalysis.SymbolEqualityComparer;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests the invariants that hold between implementations of the code model which are required to agree: the
/// conversions of nullability on a type read from source and on a type an aspect introduced, and the resolution of a
/// type identifier to a symbol and to a type of the code model.
/// </summary>
/// <remarks>
/// Each of these is a place where the same question is answered twice by different code. Issues #1835, #1837, #1838
/// and #1839 were all defects of that shape, so the invariants are asserted rather than left to be discovered by the
/// code that happens to depend on them.
/// </remarks>
public sealed class CodeModelConsistencyTests : UnitTestClass
{
    public CodeModelConsistencyTests( ITestOutputHelper? logger ) : base( logger ) { }

    /// <summary>
    /// Introduces a type into a mutable compilation and returns the resulting declaration.
    /// </summary>
    private static INamedType IntroduceType( CompilationModel compilation, string name, TypeKind typeKind, bool addTypeParameter = false )
    {
        var typeBuilder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, name, typeKind );

        if ( addTypeParameter )
        {
            typeBuilder.AddTypeParameter( "T" );
        }

        typeBuilder.Freeze();
        compilation.AddTransformation( typeBuilder.CreateTransformation() );

        return compilation.GlobalNamespace.Types.OfName( name ).Single();
    }

    private static IType GetIntroducedType( CompilationModel compilation, string shape )
        => shape switch
        {
            "Class" => IntroduceType( compilation, "IntroducedClass", TypeKind.Class ),
            "Struct" => IntroduceType( compilation, "IntroducedStruct", TypeKind.Struct ),
            "TypeParameter" => IntroduceType( compilation, "IntroducedGeneric", TypeKind.Class, addTypeParameter: true ).TypeParameters[0],
            "Array" => IntroduceType( compilation, "IntroducedArrayElement", TypeKind.Class ).MakeArrayType(),
            _ => throw new AssertionFailedException( $"Unknown shape '{shape}'." )
        };

    /// <summary>
    /// Verifies that <see cref="IType.ToNonNullable"/> on a type an aspect introduced never returns a type that is
    /// more nullable than the one it was called on, and that it undoes <see cref="IType.ToNullable"/>.
    /// </summary>
    /// <remarks>
    /// Every shape of introduced type is covered by the same invariant, because each implements the conversion
    /// separately, by a branch on whether the type is a reference type, and the branches are easy to mis-copy from
    /// one implementation to another.
    /// </remarks>
    [Theory]
    [InlineData( "Class" )]
    [InlineData( "Struct" )]
    [InlineData( "TypeParameter" )]
    [InlineData( "Array" )]
    public void ToNonNullableOnAnIntroducedTypeDoesNotIncreaseNullability( string shape )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class Outer;" ).CreateMutableClone();

        var type = GetIntroducedType( compilation, shape );

        var nullable = type.ToNullable();
        var nonNullable = nullable.ToNonNullable();

        this.TestOutput.WriteLine(
            $"{shape}: type={type.ToDisplayString()}/{Describe( type )} nullable={nullable.ToDisplayString()}/{Describe( nullable )} "
            + $"nonNullable={nonNullable.ToDisplayString()}/{Describe( nonNullable )}" );

        Assert.NotEqual( true, nonNullable.IsNullable );
        Assert.Equal( type.ToDisplayString(), nonNullable.ToDisplayString() );
    }

    private static string Describe( IType type )
        => $"IsNullable={type.IsNullable?.ToString() ?? "null"} IsReferenceType={type.IsReferenceType?.ToString() ?? "null"}";

    /// <summary>
    /// Verifies that a type read from source resolves from its own identifier to the same type, nullable annotations
    /// included, whether it was written in an annotated nullable context or in an unannotated one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The identifier records the context in a single marker, so the corpus has to contain a type whose outermost
    /// type reference says nothing about the context. A value type is not annotated in either context, so
    /// <c>KeyValuePair&lt;string, string&gt;</c> and a tuple are uninformative at their outermost type reference, and an
    /// annotated reference type says that the type reference is nullable rather than that the context was unannotated, which
    /// <c>List&lt;string&gt;?</c> covers. Reading the outermost type reference alone left the reference types nested in each
    /// of those oblivious.
    /// </para>
    /// <para>
    /// The array is here because the element of an array lost its annotation separately, the annotation having to be
    /// passed to <c>CreateArrayTypeSymbol</c> explicitly.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( "enable" )]
    [InlineData( "disable" )]
    public void ATypeResolvesFromItsIdentifierToItself( string nullableContext )
    {
        using var testContext = this.CreateTestContext();

        var code = $$"""
                     #nullable {{nullableContext}}
                     using System.Collections.Generic;

                     struct S { }

                     class C
                     {
                         public int Int;
                         public S Struct;
                         public string String = null!;
                         public string? NullableString;
                         public List<string> ListOfString = null!;
                         public List<string>? NullableListOfString;
                         public List<string?> ListOfNullableString = null!;
                         public KeyValuePair<string, string> StructOverReferences;
                         public KeyValuePair<int, int> StructOverValueTypes;
                         public (string A, string B) Tuple;
                         public string[] ArrayOfString = null!;
                         public string[][] ArrayOfArrayOfString = null!;
                     }
                     """;

        var compilation = testContext.CreateCompilationModel( code );

        var mismatches = new System.Collections.Generic.List<string>();

        foreach ( var field in compilation.Types.OfName( "C" ).Single().Fields.Where( f => !f.IsImplicitlyDeclared ) )
        {
            var symbol = field.Type.GetSymbol()!;
            var id = symbol.GetSerializableTypeId();
            var roundTrip = compilation.CompilationContext.SerializableTypeIdResolver.ResolveId( id );

            this.TestOutput.WriteLine( $"{field.Name}: {id.Id}  [{DescribeAnnotations( symbol )}] -> [{DescribeAnnotations( roundTrip )}]" );

            if ( !SymbolEqualityComparer.IncludeNullability.Equals( symbol, roundTrip ) )
            {
                mismatches.Add( $"{field.Name}: {id.Id} gave [{DescribeAnnotations( roundTrip )}] instead of [{DescribeAnnotations( symbol )}]" );
            }
        }

        foreach ( var mismatch in mismatches )
        {
            this.TestOutput.WriteLine( mismatch );
        }

        Assert.Empty( mismatches );
    }

    /// <summary>
    /// Renders the nullable annotation of a type and of the type references nested in it, which the display string does not
    /// show and which is what these assertions are about.
    /// </summary>
    private static string DescribeAnnotations( Microsoft.CodeAnalysis.ITypeSymbol symbol )
    {
        var text = symbol.NullableAnnotation.ToString();

        switch ( symbol )
        {
            case Microsoft.CodeAnalysis.INamedTypeSymbol { TypeArguments.Length: > 0 } namedType:
                text += "<" + string.Join( ",", namedType.TypeArguments.Select( DescribeAnnotations ) ) + ">";

                break;

            case Microsoft.CodeAnalysis.IArrayTypeSymbol arrayType:
                text += "[" + DescribeAnnotations( arrayType.ElementType ) + "]";

                break;
        }

        return text;
    }

    /// <summary>
    /// Verifies that the nullable form of a type an aspect introduced survives being written as a
    /// <see cref="SerializableTypeId"/> and resolved again.
    /// </summary>
    /// <remarks>
    /// An aspect that introduces a type and then uses it as the type of a member it introduces reaches this, because
    /// the type is carried as a durable reference from the aspect that builds it to the code that emits the member.
    /// Losing the annotation there produces a member declared with a non-nullable type and initialized with
    /// <c>default</c>, which the compiler reports as CS8625.
    /// </remarks>
    [Theory]
    [InlineData( "Class" )]
    [InlineData( "Struct" )]
    [InlineData( "Array" )]
    public void TheNullableFormOfAnIntroducedTypeSurvivesItsTypeId( string shape )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class Outer;" ).CreateMutableClone();

        var nullable = GetIntroducedType( compilation, shape ).ToNullable();

        var id = nullable.GetSerializableTypeId();
        var roundTrip = compilation.SerializableTypeIdResolver.ResolveId( id );

        this.TestOutput.WriteLine( $"{shape}: {nullable.ToDisplayString()}/{Describe( nullable )} -> {id.Id}" );
        this.TestOutput.WriteLine( $"    resolved to {roundTrip.ToDisplayString()}/{Describe( roundTrip )}" );

        Assert.Equal( true, roundTrip.IsNullable );
        Assert.Equal( nullable.ToDisplayString(), roundTrip.ToDisplayString() );
    }

    /// <summary>
    /// Verifies that the nullable form of a type an aspect introduced survives being turned into a reference and back,
    /// both through the reference that <see cref="IDeclaration.ToRef"/> returns and through the durable form of it.
    /// </summary>
    /// <remarks>
    /// Advice carries the type of a member it introduces as a reference, and the pull strategy of a parameter carries
    /// it as a durable one, so a reference that cannot express the annotation drops it before the member is emitted.
    /// </remarks>
    [Theory]
    [InlineData( "Class" )]
    [InlineData( "Struct" )]
    public void TheNullableFormOfAnIntroducedTypeSurvivesItsReference( string shape )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class Outer;" ).CreateMutableClone();

        var nullable = (INamedType) GetIntroducedType( compilation, shape ).ToNullable();

        var throughRef = nullable.ToRef().GetTarget( compilation );
        var throughDurableRef = nullable.ToRef().ToDurable().GetTarget( compilation );

        // A durable reference is identified by its identifier alone, so the annotation is only durable if it survives
        // being written to that string and read back from it.
        var id = nullable.ToRef().ToDurable().ToSerializableId();
        var throughId = (IType) id.Resolve( compilation );

        this.TestOutput.WriteLine( $"{shape}: nullable={nullable.ToDisplayString()}/{Describe( nullable )}" );
        this.TestOutput.WriteLine( $"    through a reference: {throughRef.ToDisplayString()}/{Describe( throughRef )}" );
        this.TestOutput.WriteLine( $"    through a durable reference: {throughDurableRef.ToDisplayString()}/{Describe( throughDurableRef )}" );
        this.TestOutput.WriteLine( $"    through the identifier '{id}': {throughId.ToDisplayString()}/{Describe( throughId )}" );

        Assert.Equal( true, throughRef.IsNullable );
        Assert.Equal( true, throughDurableRef.IsNullable );
        Assert.Equal( true, throughId.IsNullable );
    }

    /// <summary>
    /// Establishes that a reference to a type read from source carries the nullable annotation, which is the behaviour
    /// that the reference to an introduced type has to match.
    /// </summary>
    [Fact]
    public void TheNullableFormOfASourceTypeSurvivesItsReference()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class SourceClass;" );

        var nullable = compilation.Types.OfName( "SourceClass" ).Single().ToNullable();

        var throughRef = nullable.ToRef().GetTarget( compilation );
        var throughDurableRef = nullable.ToRef().ToDurable().GetTarget( compilation );

        this.TestOutput.WriteLine( $"source: nullable={nullable.ToDisplayString()}/{Describe( nullable )}" );
        this.TestOutput.WriteLine( $"    through a reference: {throughRef.ToDisplayString()}/{Describe( throughRef )}" );
        this.TestOutput.WriteLine( $"    through a durable reference: {throughDurableRef.ToDisplayString()}/{Describe( throughDurableRef )}" );

        Assert.Equal( true, throughRef.IsNullable );
        Assert.Equal( true, throughDurableRef.IsNullable );
    }

    /// <summary>
    /// Verifies that <see cref="IType.ToNullable"/> on a value type produces the same shape of type whether the value
    /// type was read from source or introduced by an aspect.
    /// </summary>
    /// <remarks>
    /// The implementation for a type read from source constructs <see cref="System.Nullable{T}"/>, whereas the
    /// implementation for an introduced type sets a flag on the type itself. An aspect that asks the same question of
    /// a type it introduced and of a type it read therefore gets answers that differ in the name of the type, in
    /// whether the result is a constructed generic type, and in how it is displayed.
    /// </remarks>
    [Fact]
    public void ToNullableOnAValueTypeAgreesBetweenSourceAndIntroduced()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "struct SourceStruct { }" ).CreateMutableClone();

        var sourceStruct = compilation.GlobalNamespace.Types.OfName( "SourceStruct" ).Single();
        var introducedStruct = IntroduceType( compilation, "IntroducedStruct", TypeKind.Struct );

        var sourceNullable = sourceStruct.ToNullable();
        var introducedNullable = introducedStruct.ToNullable();

        this.TestOutput.WriteLine(
            $"source:     {sourceNullable.ToDisplayString()} name={sourceNullable.Name} "
            + $"specialType={sourceNullable.SpecialType} typeArguments={sourceNullable.TypeArguments.Count} isNullable={sourceNullable.IsNullable}" );

        this.TestOutput.WriteLine(
            $"introduced: {introducedNullable.ToDisplayString()} name={introducedNullable.Name} "
            + $"specialType={introducedNullable.SpecialType} typeArguments={introducedNullable.TypeArguments.Count} "
            + $"isNullable={introducedNullable.IsNullable}" );

        Assert.Equal( sourceNullable.IsNullable, introducedNullable.IsNullable );
        Assert.Equal( sourceNullable.Name, introducedNullable.Name );
        Assert.Equal( sourceNullable.SpecialType, introducedNullable.SpecialType );
        Assert.Equal( sourceNullable.TypeArguments.Count, introducedNullable.TypeArguments.Count );
    }

    /// <summary>
    /// Verifies that the identifier of a tuple type resolves to an equivalent tuple type, including the names of its
    /// elements and whatever its arity.
    /// </summary>
    /// <remarks>
    /// The identifier is written in tuple syntax, which carries the name of each element, and the resolver of the
    /// code model rebuilds the type by looking up <c>System.ValueTuple</c> of the arity of the tuple. That type is
    /// declared for the arities one to eight, the eighth parameter of which holds the remaining elements and must
    /// itself be a tuple, so a tuple of more than seven elements cannot be rebuilt by a lookup of its arity alone.
    /// <c>DeclarationFactory</c> nests such a tuple correctly, which is what the resolver is compared against here.
    /// </remarks>
    [Theory]
    [InlineData( "(int, string)" )]
    [InlineData( "(int Count, string Name)" )]
    [InlineData( "(int, int, int, int, int, int, int, int)" )]
    [InlineData( "(int, int, int, int, int, int, int, int, int)" )]
    public void TupleIdentifierResolvesToAnEquivalentTuple( string type )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( $"class C {{ public {type} F; }}" );

        var fieldType = compilation.Types.OfName( "C" ).Single().Fields.OfName( "F" ).Single().Type;
        Assert.IsAssignableFrom<ITupleType>( fieldType );

        var id = fieldType.GetSerializableTypeId();
        this.TestOutput.WriteLine( $"{type}: {id.Id}" );

        var roundTripType = compilation.SerializableTypeIdResolver.ResolveId( id );

        this.TestOutput.WriteLine( $"    resolved to {roundTripType.ToDisplayString()}" );

        Assert.Equal( fieldType.ToDisplayString(), roundTripType.ToDisplayString() );
        Assert.IsAssignableFrom<ITupleType>( roundTripType );
    }

    private const string _resolverCorpusCode = """
                                               using System.Collections.Generic;

                                               struct S { }

                                               class Generic<T>
                                               {
                                                   public class Nested { }
                                               }

                                               class C
                                               {
                                                   public Generic<int> Constructed = null!;
                                                   public Generic<int>.Nested ConstructedNested = null!;
                                                   public object Object = null!;
                                                   public object? NullableObject;
                                                   public S Struct;
                                                   public S? NullableStruct;
                                                   public int[] Array = null!;
                                                   public List<List<string?>> Deep = null!;
                                                   public (int Count, string Name) Tuple;
                                               }
                                               """;

    /// <summary>
    /// Verifies that the resolver of symbols and the resolver of the code model resolve the same identifier to the
    /// same type.
    /// </summary>
    /// <remarks>
    /// A durable reference uses whichever of the two the caller asks for, so the two have to agree. They are separate
    /// implementations of one abstract resolver, and every defect of issues #1835, #1837 and #1839 was a place where
    /// they did not. The corpus covers a generic definition and a construction of it, because the two implementations
    /// normalize a construction whose arguments are the type parameters differently.
    /// </remarks>
    [Theory]
    [InlineData( "Generic" )]
    [InlineData( "C.Constructed" )]
    [InlineData( "C.ConstructedNested" )]
    [InlineData( "C.Object" )]
    [InlineData( "C.NullableObject" )]
    [InlineData( "C.Struct" )]
    [InlineData( "C.NullableStruct" )]
    [InlineData( "C.Array" )]
    [InlineData( "C.Deep" )]
    [InlineData( "C.Tuple" )]
    public void TheTwoResolversResolveAnIdentifierToTheSameType( string path )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _resolverCorpusCode );

        var parts = path.Split( '.' );

        var type = parts.Length == 1
            ? compilation.Types.OfName( parts[0] ).Single()
            : compilation.Types.OfName( parts[0] ).Single().Fields.OfName( parts[1] ).Single().Type;

        var id = type.GetSerializableTypeId( includeGenericContext: true );
        this.TestOutput.WriteLine( $"{path}: {id.Id}" );

        var fromSymbolResolver = compilation.CompilationContext.SerializableTypeIdResolver.ResolveId( id );
        var fromCodeModelResolver = compilation.SerializableTypeIdResolver.ResolveId( id ).GetSymbol();

        this.TestOutput.WriteLine(
            $"    symbol resolver: {fromSymbolResolver} [{DescribeSymbol( fromSymbolResolver )}], "
            + $"code model resolver: {fromCodeModelResolver} [{DescribeSymbol( fromCodeModelResolver )}]" );

        Assert.Equal( fromSymbolResolver, fromCodeModelResolver, SymbolEqualityComparer.IncludeNullability );
    }

    /// <summary>
    /// Formats the nullability of a symbol and of its type arguments, so that a failure reports where two symbols
    /// that are displayed identically actually differ.
    /// </summary>
    private static string DescribeSymbol( Microsoft.CodeAnalysis.ISymbol? symbol )
        => symbol is Microsoft.CodeAnalysis.ITypeSymbol type
            ? $"{type.NullableAnnotation}"
              + (symbol is Microsoft.CodeAnalysis.INamedTypeSymbol { TypeArguments.Length: > 0 } named
                  ? $" arguments={string.Join( ",", named.TypeArguments.Select( a => $"{a}:{a.NullableAnnotation}" ) )}"
                  : "")
            : "not a type";

    /// <summary>
    /// Verifies that the serializable identifier of a method that takes a named tuple resolves back to that method.
    /// </summary>
    /// <remarks>
    /// A declaration identifier is a documentation identifier, which renders a tuple as the ValueTuple it is and
    /// cannot express the names of its elements. Resolving it therefore has to match the parameter by a comparison
    /// that ignores those names, which is the identity conversion and not the equality of two types. See issue #1844.
    /// </remarks>
    [Theory]
    [InlineData( "(int x, int y)" )]
    [InlineData( "(int, int)" )]
    public void DeclarationIdOfAMethodTakingATupleResolves( string parameterType )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( $"class C {{ public void M( {parameterType} p ) {{ }} }}" );

        var method = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M" ).Single();

        var id = method.ToSerializableId();
        this.TestOutput.WriteLine( id.Id );

        Assert.NotNull( id.ResolveToDeclaration( compilation ) );
    }
}
