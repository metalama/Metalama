// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using SymbolEqualityComparer = Microsoft.CodeAnalysis.SymbolEqualityComparer;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

public sealed class SerializableTypeIdTests : UnitTestClass
{
    public SerializableTypeIdTests( ITestOutputHelper? logger ) : base( logger ) { }

    [Theory]
    [InlineData( typeof(int) )]
    [InlineData( typeof(void) )]
    [InlineData( typeof(object) )]
    [InlineData( typeof(object[]) )]
    [InlineData( typeof(int*) )]
    [InlineData( typeof(int[]) )]
    [InlineData( typeof(decimal) )]
    [InlineData( typeof(List<decimal>) )]
    [InlineData( typeof(List<int[]>) )]
    [InlineData( typeof(List<>) )]
    [InlineData( typeof((int, string)) )]
    [InlineData( typeof(Dictionary<,>) )]
    [InlineData( typeof(Dictionary<List<string>, List<int>>) )]
    public void TestTypeOf( Type type )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );

        var iType = compilation.Factory.GetTypeByReflectionType( type );

        foreach ( var bypassSymbols in new[] { false, true } )
        {
            var id = iType.GetSerializableTypeId( bypassSymbols: bypassSymbols );
            this.TestOutput.WriteLine( id.Id );

            var roundTripSymbol = compilation.CompilationContext.SerializableTypeIdResolver.ResolveId( id );
            Assert.Equal( iType.GetSymbol(), roundTripSymbol, SymbolEqualityComparer.Default );

            var roundTripType = compilation.SerializableTypeIdResolver.ResolveId( id );
            Assert.Same( iType, roundTripType );
        }
    }

    [Theory]
    [InlineData( "object" )]
    [InlineData( "object?" )]
    [InlineData( "Task<object>" )]
    [InlineData( "Task<object?>" )]
    public void TestNullableType( string type )
    {
        using var testContext = this.CreateTestContext();

        var code = $"using System.Threading.Tasks;"
                   + $"class C {{ {type} f; }}";

        var compilation = testContext.CreateCompilationModel( code );

        var iType = compilation.Types.Single().Fields.Single().Type;

        foreach ( var bypassSymbols in new[] { false, true } )
        {
            var typeId = iType.GetSerializableTypeId( bypassSymbols: bypassSymbols );

            var roundTripSymbol = compilation.CompilationContext.SerializableTypeIdResolver.ResolveId( typeId );
            Assert.Equal( iType.GetSymbol(), roundTripSymbol, SymbolEqualityComparer.IncludeNullability );

            var roundTripType = compilation.SerializableTypeIdResolver.ResolveId( typeId );
            Assert.Same( iType, roundTripType );
        }
    }

    private const string _constrainedGenericTypesCode = """
                                                        using System.Collections.Generic;

                                                        class StructConstrained<T> where T : struct
                                                        {
                                                            public List<T> Field = null!;
                                                        }

                                                        class UnmanagedConstrained<T> where T : unmanaged { }

                                                        class ClassConstrained<T> where T : class
                                                        {
                                                            public List<T> Field = null!;
                                                        }

                                                        class NotNullConstrained<T> where T : notnull
                                                        {
                                                            public List<T> Field = null!;
                                                        }

                                                        class Unconstrained<T>
                                                        {
                                                            public List<T> Field = null!;
                                                        }
                                                        """;

    /// <summary>
    /// Verifies that the identifier of a generic type definition resolves to an equivalent type through both
    /// resolvers, whatever constrains its type parameter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The identifier is built the way <c>FullRef.CreateDurableRef</c> builds it, with its generic context, because
    /// the context is what makes the type parameter resolve through the dictionary of generic arguments rather than
    /// through the short circuit that recognizes a parameter of the declaration being resolved. Only the former path
    /// applies the nullability of the outermost type to the parameter, which is what threw for a parameter
    /// constrained to be a value type. See issue #1835.
    /// </para>
    /// <para>
    /// Both resolvers are exercised on the same identifier, because a durable reference uses either depending on what
    /// it is asked for: <c>TypeIdRef.GetSymbol</c> and <c>TypeIdRef.ToFullRef</c> resolve to a symbol and
    /// <c>TypeIdRef.Resolve</c> to a type of the code model. They answered differently in issue #1835, the symbol
    /// resolver accepting an identifier that the code model resolver rejected. See issue #1837.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( "StructConstrained" )]
    [InlineData( "UnmanagedConstrained" )]
    [InlineData( "ClassConstrained" )]
    [InlineData( "NotNullConstrained" )]
    [InlineData( "Unconstrained" )]
    public void TestGenericDefinitionWithConstrainedTypeParameter( string typeName )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _constrainedGenericTypesCode );

        var type = compilation.Types.OfName( typeName ).Single();

        this.AssertRoundTrip( compilation, type );
    }

    /// <summary>
    /// Verifies that the identifier of a type that mentions a type parameter, in a position other than the parameter
    /// list of the declaration being resolved, resolves to an equivalent type through both resolvers, whatever
    /// constrains the parameter.
    /// </summary>
    /// <remarks>
    /// The type argument here is annotated as non-nullable in the source, whatever the constraint, so the resolver
    /// has to reproduce that annotation rather than remove it. Removing it is what the code model resolver did, which
    /// only a parameter that is not constrained to be a value type revealed: the declaration of a value-type
    /// constrained parameter is annotated already, so removing the annotation and setting it agreed. See issue #1839.
    /// </remarks>
    [Theory]
    [InlineData( "StructConstrained" )]
    [InlineData( "ClassConstrained" )]
    [InlineData( "NotNullConstrained" )]
    [InlineData( "Unconstrained" )]
    public void TestTypeMentioningATypeParameter( string typeName )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _constrainedGenericTypesCode );

        var fieldType = compilation.Types.OfName( typeName ).Single().Fields.OfName( "Field" ).Single().Type;
        Assert.Equal( "List<T>", fieldType.ToDisplayString() );

        this.AssertRoundTrip( compilation, fieldType );
    }

    /// <summary>
    /// Verifies that the identifier of a type parameter itself resolves to an equivalent type through both resolvers,
    /// whatever constrains it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A type parameter has no identifier other than its name, so its generic context is the whole of what makes it
    /// resolvable.
    /// </para>
    /// <para>
    /// The identifier of a type parameter carries no nullability marker, whatever the constraint answers about the
    /// nullability of the parameter, because the parameter is resolved from the generic context and the marker can
    /// only contradict what the context declares. See <c>SerializableTypeIdGenerator.CanBeAnnotated</c>.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( "StructConstrained" )]
    [InlineData( "UnmanagedConstrained" )]
    [InlineData( "ClassConstrained" )]
    [InlineData( "NotNullConstrained" )]
    [InlineData( "Unconstrained" )]
    public void TestTypeParameter( string typeName )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _constrainedGenericTypesCode );

        var typeParameter = compilation.Types.OfName( typeName ).Single().TypeParameters[0];

        Assert.DoesNotContain( '!', typeParameter.GetSerializableTypeId( includeGenericContext: true ).Id );

        this.AssertRoundTrip( compilation, typeParameter );
    }

    /// <summary>
    /// Verifies that the identifier of a value type that is not a <see cref="Nullable{T}"/>, and of the
    /// <see cref="Nullable{T}"/> of it, resolve to equivalent types through both resolvers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A value type declared in source is written into the identifier by name, and a name is where the nullability of
    /// the outermost type is applied. A value type built into the language, which
    /// <see cref="TestTypeOf(System.Type)"/> covers, is written as a keyword instead and takes a different branch of
    /// the resolver, which annotates only <c>object</c> and <c>string</c>.
    /// </para>
    /// <para>
    /// The type is covered both as the outermost type and as the type argument of a generic type. Only the latter is
    /// annotated: the identifier carries the trailing <c>!</c> only when the outermost type is a reference type, so
    /// the position in which a value type is annotated is the one it does not occupy itself.
    /// </para>
    /// <para>
    /// The <see cref="Nullable{T}"/> of the type is covered in both positions as well, because Roslyn annotates the
    /// symbol of a value type written as <c>T?</c> whereas constructing <see cref="Nullable{T}"/> does not, so the
    /// resolver has to set the annotation itself.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( "Struct" )]
    [InlineData( "Struct?" )]
    [InlineData( "System.Collections.Generic.List<Struct>" )]
    [InlineData( "System.Collections.Generic.List<Struct?>" )]
    public void TestValueTypeDeclaredInSource( string type )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( $"struct Struct {{ }} class C {{ {type} F; }}" );

        var fieldType = compilation.Types.OfName( "C" ).Single().Fields.OfName( "F" ).Single().Type;

        this.AssertRoundTrip( compilation, fieldType );
    }

    /// <summary>
    /// Asserts that the identifier of a type, built the way a durable reference builds it, resolves to an equivalent
    /// type through the resolver of symbols and through the resolver of the code model alike.
    /// </summary>
    private void AssertRoundTrip( CompilationModel compilation, IType type )
    {
        foreach ( var bypassSymbols in new[] { false, true } )
        {
            var id = type.GetSerializableTypeId( includeGenericContext: true, bypassSymbols: bypassSymbols );
            this.TestOutput.WriteLine( id.Id );

            var roundTripSymbol = compilation.CompilationContext.SerializableTypeIdResolver.ResolveId( id );
            Assert.Equal( type.GetSymbol(), roundTripSymbol, SymbolEqualityComparer.IncludeNullability );

            var roundTripType = compilation.SerializableTypeIdResolver.ResolveId( id );
            Assert.Equal( type, roundTripType, compilation.Comparers.IncludeNullability );
        }
    }

    [Theory]
    [InlineData( "Y:x" )]
    [InlineData( "Y:+" )]
    [InlineData( "Y:List<x>" )]
    public void TestInvalidString( string s )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" );

        // We are testing that the method gracefully fails.
        Assert.False( compilation.CompilationContext.SerializableTypeIdResolver.TryResolveId( new SerializableTypeId( s ), out _ ) );
        Assert.False( compilation.SerializableTypeIdResolver.TryResolveId( new SerializableTypeId( s ), out _ ) );
    }
}