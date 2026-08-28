// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

/// <summary>
/// Covers <see cref="CompileTimeTypeFactory"/> and the <see cref="CompileTimeType"/> hierarchy.
/// </summary>
/// <remarks>
/// The point of the hierarchy is that a mock can answer structural questions about the type it stands for
/// <em>without</em> a compilation, which is what allows it to be serialized without one. These tests therefore assert
/// the structural surface (kind, element type, rank, generic arguments, assembly name), not just the names.
/// </remarks>
public sealed class CompileTimeTypeFactoryTests : UnitTestClass
{
    private const string _code = """
                                 namespace Ns
                                 {
                                     public class SimpleClass { }
                                     public struct SimpleStruct { }
                                     public enum SimpleEnum { A }
                                     public interface ISimpleInterface { }
                                     public class Generic<T> { }
                                     public class Outer { public class Nested { } }

                                     public class Holder
                                     {
                                         public SimpleClass[] Array1D;
                                         public SimpleClass[,] Array2D;
                                         public SimpleClass[][] JaggedArray;
                                         public Generic<SimpleClass> ConstructedGeneric;
                                         public Generic<Generic<SimpleClass>> NestedConstructedGeneric;
                                         public SimpleEnum[] EnumArray;
                                         public Outer.Nested NestedType;
                                         public unsafe int* Pointer;
                                     }

                                     public class GenericHolder<T> { public T Field; }
                                 }
                                 """;

    private static CompilationModel CreateCompilation( TestContext testContext ) => testContext.CreateCompilationModel( _code );

    private static CompileTimeTypeFactory GetFactory( CompilationModel compilation ) => compilation.CompilationContext.CompileTimeTypeFactory;

    private static CompileTimeType GetMock( CompilationModel compilation, IType type )
        => GetFactory( compilation ).Get( type.GetSymbol().AssertSymbolNotNull() );

    private static IType GetFieldType( CompilationModel compilation, string typeName, string fieldName )
        => compilation.Types.OfName( typeName ).Single().Fields.OfName( fieldName ).Single().Type;

    private static INamedType GetType( CompilationModel compilation, string typeName ) => compilation.Types.OfName( typeName ).Single();

    [Fact]
    public void GetType_Class_IsNamedTypeAndCarriesItsAssembly()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var mock = GetMock( compilation, GetType( compilation, "SimpleClass" ) );

        var named = Assert.IsType<CompileTimeNamedType>( mock );
        Assert.False( named.IsEnum );
        Assert.False( named.IsValueType );
        Assert.False( named.IsArray );
        Assert.False( named.IsPointer );
        Assert.False( named.IsGenericType );
        Assert.Equal( "Ns", named.Namespace );
        Assert.Equal( "SimpleClass", named.Name );
        Assert.Equal( "Ns.SimpleClass", named.FullName );
        Assert.Equal( compilation.RoslynCompilation.AssemblyName, named.AssemblyName );
    }

    [Fact]
    public void GetType_Struct_IsValueTypeButNotEnum()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var named = Assert.IsType<CompileTimeNamedType>( GetMock( compilation, GetType( compilation, "SimpleStruct" ) ) );

        Assert.True( named.IsValueType );
        Assert.False( named.IsEnum );
    }

    [Fact]
    public void GetType_Enum_IsEnumAndValueType()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var named = Assert.IsType<CompileTimeNamedType>( GetMock( compilation, GetType( compilation, "SimpleEnum" ) ) );

        // IsEnum is the question the serializer asks first (see SerializationIntrinsicTypeExtensions.GetIntrinsicType);
        // it used to throw on a mock, which is why the writer had to resolve the mock back to a symbol.
        Assert.True( named.IsEnum );
        Assert.True( named.IsValueType );
    }

    [Fact]
    public void GetType_Interface_IsNeitherEnumNorValueType()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var named = Assert.IsType<CompileTimeNamedType>( GetMock( compilation, GetType( compilation, "ISimpleInterface" ) ) );

        Assert.False( named.IsEnum );
        Assert.False( named.IsValueType );
    }

    [Fact]
    public void GetType_Array_ExposesElementTypeAndRank()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var mock = GetMock( compilation, GetFieldType( compilation, "Holder", "Array1D" ) );

        var array = Assert.IsType<CompileTimeArrayType>( mock );
        Assert.True( array.IsArray );
        Assert.True( array.HasElementType );
        Assert.Equal( 1, array.GetArrayRank() );

        // The element is itself a cached mock, not a flattened name.
        var element = Assert.IsType<CompileTimeNamedType>( array.GetElementType() );
        Assert.Equal( "Ns.SimpleClass", element.FullName );
    }

    [Fact]
    public void GetType_MultiDimensionalArray_ExposesRank()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var array = Assert.IsType<CompileTimeArrayType>( GetMock( compilation, GetFieldType( compilation, "Holder", "Array2D" ) ) );

        Assert.Equal( 2, array.GetArrayRank() );
    }

    [Fact]
    public void GetType_JaggedArray_NestsArrayMocks()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var outer = Assert.IsType<CompileTimeArrayType>( GetMock( compilation, GetFieldType( compilation, "Holder", "JaggedArray" ) ) );
        var inner = Assert.IsType<CompileTimeArrayType>( outer.GetElementType() );

        Assert.Equal( 1, outer.GetArrayRank() );
        Assert.Equal( 1, inner.GetArrayRank() );
        Assert.IsType<CompileTimeNamedType>( inner.GetElementType() );
    }

    [Fact]
    public void GetType_Array_InheritsAssemblyNameFromElement()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var array = Assert.IsType<CompileTimeArrayType>( GetMock( compilation, GetFieldType( compilation, "Holder", "Array1D" ) ) );

        // An array has no declaring assembly of its own; serialization must name it after its element type.
        Assert.Equal( compilation.RoslynCompilation.AssemblyName, array.AssemblyName );
    }

    [Fact]
    public void GetType_ArrayOfEnum_ElementIsEnum()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var array = Assert.IsType<CompileTimeArrayType>( GetMock( compilation, GetFieldType( compilation, "Holder", "EnumArray" ) ) );
        var element = Assert.IsType<CompileTimeNamedType>( array.GetElementType() );

        Assert.True( element.IsEnum );
        Assert.False( array.IsEnum );
    }

    [Fact]
    public void GetType_ConstructedGeneric_ExposesDefinitionAndArguments()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var mock = GetMock( compilation, GetFieldType( compilation, "Holder", "ConstructedGeneric" ) );

        var named = Assert.IsType<CompileTimeNamedType>( mock );
        Assert.True( named.IsGenericType );
        Assert.False( named.IsGenericTypeDefinition );

        var argument = Assert.Single( named.GetGenericArguments() );
        Assert.Equal( "Ns.SimpleClass", argument.FullName );

        var definition = Assert.IsType<CompileTimeNamedType>( named.GetGenericTypeDefinition() );
        Assert.True( definition.IsGenericTypeDefinition );
    }

    [Fact]
    public void GetType_NestedConstructedGeneric_NestsArgumentMocks()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var outer = Assert.IsType<CompileTimeNamedType>( GetMock( compilation, GetFieldType( compilation, "Holder", "NestedConstructedGeneric" ) ) );
        var inner = Assert.IsType<CompileTimeNamedType>( Assert.Single( outer.GetGenericArguments() ) );

        Assert.True( inner.IsGenericType );
        Assert.Equal( "Ns.SimpleClass", Assert.Single( inner.GetGenericArguments() ).FullName );
    }

    [Fact]
    public void GetType_OpenGenericDefinition_IsDistinctFromNonGenericOfSameName()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var definition = Assert.IsType<CompileTimeNamedType>( GetMock( compilation, GetType( compilation, "Generic" ) ) );
        var simple = Assert.IsType<CompileTimeNamedType>( GetMock( compilation, GetType( compilation, "SimpleClass" ) ) );

        // The arity is what distinguishes Generic`1 from a non-generic type; losing it would collide them in the cache.
        Assert.True( definition.IsGenericTypeDefinition );
        Assert.False( simple.IsGenericTypeDefinition );
        Assert.NotEqual( definition.FullName, simple.FullName );
        Assert.NotSame( definition, simple );
    }

    [Fact]
    public void GetType_GenericParameter_ExposesDeclaringTypeAndPosition()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var mock = GetMock( compilation, GetFieldType( compilation, "GenericHolder", "Field" ) );

        var parameter = Assert.IsType<CompileTimeGenericParameterType>( mock );
        Assert.True( parameter.IsGenericParameter );
        Assert.False( parameter.IsGenericType );
        Assert.Equal( 0, parameter.GenericParameterPosition );
        Assert.NotNull( parameter.DeclaringType );
    }

    [Fact]
    public void GetType_NestedType_IsNamedType()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var named = Assert.IsType<CompileTimeNamedType>( GetMock( compilation, GetFieldType( compilation, "Holder", "NestedType" ) ) );

        Assert.Equal( "Nested", named.Name );
        Assert.Contains( "Outer", named.FullName, StringComparison.Ordinal );
    }

    [Fact]
    public void GetType_NestedType_MatchesReflectionShape()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var nested = Assert.IsType<CompileTimeNamedType>( GetMock( compilation, GetFieldType( compilation, "Holder", "NestedType" ) ) );

        // Reflection separates a nested type from its declaring type with '+', reports the *declaring* type's namespace
        // on it, and reports only the simple name in Name. This is the shape the serialization format has always stored.
        Assert.Equal( "Ns.Outer+Nested", nested.FullName );
        Assert.Equal( "Ns", nested.Namespace );
        Assert.Equal( "Nested", nested.Name );

        Assert.True( nested.IsNested );

        var declaring = Assert.IsType<CompileTimeNamedType>( nested.DeclaringType );
        Assert.Equal( "Ns.Outer", declaring.FullName );
        Assert.False( declaring.IsNested );
        Assert.Null( declaring.DeclaringType );
    }

    [Fact]
    public void GetType_NonNestedType_HasNoDeclaringType()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var named = Assert.IsType<CompileTimeNamedType>( GetMock( compilation, GetType( compilation, "SimpleClass" ) ) );

        Assert.False( named.IsNested );
        Assert.Null( named.DeclaringType );
    }

    [Fact]
    public void GetType_NestedType_DeclaringTypeIsTheCachedMock()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var nestedSymbol = GetFieldType( compilation, "Holder", "NestedType" ).GetSymbol().AssertSymbolNotNull();
        var nested = (CompileTimeNamedType) GetFactory( compilation ).Get( nestedSymbol );

        // The declaring type is built from the containing symbol, and must come out of the cache like any other level.
        var declaring = Assert.IsType<CompileTimeNamedType>( nested.DeclaringType );
        Assert.Same( GetFactory( compilation ).Get( nestedSymbol.ContainingType ), declaring );
    }

    [Fact]
    public void GetType_NullabilityIsPartOfTheTypeId()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var nestedSymbol = GetFieldType( compilation, "Holder", "NestedType" ).GetSymbol().AssertSymbolNotNull();

        // Nullability is part of a type id by design, and ContainingType is nullable-oblivious while a declaration
        // symbol is not. So the same logical type legitimately yields two ids -- and two mocks -- depending on how it
        // was reached. This is not specific to nesting; it is recorded here because a nested type is where one is most
        // likely to reach the same type by both routes and be surprised.
        var viaContainingType = GetFactory( compilation ).Get( nestedSymbol.ContainingType );
        var viaDeclaration = GetMock( compilation, GetType( compilation, "Outer" ) );

        Assert.Equal( "Ns.Outer", viaContainingType.FullName );
        Assert.Equal( "Ns.Outer", viaDeclaration.FullName );
        Assert.NotEqual( viaContainingType.TypeId.Id, viaDeclaration.TypeId.Id );
    }

    [Fact]
    public void CreateNamedType_FromWireName_RebuildsTheNesting()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        // This is what deserialization gets: a reflection full name and an assembly name, and no compilation.
        var mock = (CompileTimeNamedType) GetFactory( compilation ).CreateNamedType( "Ns.Outer+Nested", "SomeAssembly", false, false );

        Assert.Equal( "Ns.Outer+Nested", mock.FullName );
        Assert.Equal( "Nested", mock.Name );
        Assert.Equal( "Ns", mock.Namespace );
        Assert.True( mock.IsNested );

        var declaring = Assert.IsType<CompileTimeNamedType>( mock.DeclaringType );
        Assert.Equal( "Ns.Outer", declaring.FullName );
        Assert.Equal( "SomeAssembly", declaring.AssemblyName );
        Assert.False( declaring.IsNested );
    }

    [Fact]
    public void CreateNamedType_FromDeeplyNestedWireName_RebuildsEveryLevel()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var mock = (CompileTimeNamedType) GetFactory( compilation ).CreateNamedType( "Ns.A+B+C", "SomeAssembly", false, false );

        Assert.Equal( "C", mock.Name );

        var b = Assert.IsType<CompileTimeNamedType>( mock.DeclaringType );
        Assert.Equal( "B", b.Name );
        Assert.Equal( "Ns.A+B", b.FullName );

        var a = Assert.IsType<CompileTimeNamedType>( b.DeclaringType );
        Assert.Equal( "A", a.Name );
        Assert.Equal( "Ns.A", a.FullName );
        Assert.Null( a.DeclaringType );
    }

    [Fact]
    public void CreateNamedType_FromNonNestedWireName_HasNoDeclaringType()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var mock = (CompileTimeNamedType) GetFactory( compilation ).CreateNamedType( "Ns.SimpleClass", "SomeAssembly", false, false );

        Assert.False( mock.IsNested );
        Assert.Null( mock.DeclaringType );
        Assert.Equal( "Ns", mock.Namespace );
        Assert.Equal( "SimpleClass", mock.Name );
    }

    [Fact]
    public void TypeId_OfNestedType_UsesDotSeparatorAndNoDuplicateNamespace()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var nested = GetMock( compilation, GetFieldType( compilation, "Holder", "NestedType" ) );

        // A type id is C# syntax, so nesting is '.', not the reflection '+'. The namespace belongs to the outermost type
        // only: emitting it again for the nested type would give `Ns.OuterNs.Nested`.
        var id = nested.GetSerializableTypeId().Id;

        Assert.Equal( SerializableTypeId.Prefix + "global::Ns.Outer.Nested", id );
    }

    [Fact]
    public void TypeId_OfNestedGenericType_StripsArityAndKeepsNesting()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var mock = GetFactory( compilation ).CreateNamedType( "Ns.Outer+Nested", "SomeAssembly", false, false );

        Assert.Equal( SerializableTypeId.Prefix + "global::Ns.Outer.Nested", mock.GetSerializableTypeId().Id );
    }

    [Fact]
    public void GetType_SameTypeTwice_ReturnsSameInstance()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );
        var type = GetType( compilation, "SimpleClass" );

        // Instance identity is the factory's contract: production code compares mocks by reference in places.
        Assert.Same( GetMock( compilation, type ), GetMock( compilation, type ) );
    }

    [Fact]
    public void GetType_DifferentTypes_ReturnDifferentInstances()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        Assert.NotSame( GetMock( compilation, GetType( compilation, "SimpleClass" ) ), GetMock( compilation, GetType( compilation, "SimpleStruct" ) ) );
    }

    [Fact]
    public void GetType_ArrayAndItsElement_AreDistinctCacheEntries()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var array = GetMock( compilation, GetFieldType( compilation, "Holder", "Array1D" ) );
        var element = GetMock( compilation, GetType( compilation, "SimpleClass" ) );

        Assert.NotSame( array, element );

        // The array's element must be the *cached* mock, not a fresh one.
        Assert.Same( element, ((CompileTimeArrayType) array).GetElementType() );
    }

    [Fact]
    public void GetType_Equality_IsByTypeId()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );
        var type = GetType( compilation, "SimpleClass" );

        var a = GetMock( compilation, type );
        var b = GetMock( compilation, type );

        Assert.Equal( a, b );
        Assert.Equal( a.GetHashCode(), b.GetHashCode() );

        // Identity is the id and nothing else, so equality and the factory's cache key are the same thing by construction.
        Assert.Equal( a.TypeId, b.TypeId );
    }

    [Fact]
    public void TypeId_IsTheWholeIdentity_AndDistinguishesEveryKind()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        var simple = GetMock( compilation, GetType( compilation, "SimpleClass" ) );
        var array = GetMock( compilation, GetFieldType( compilation, "Holder", "Array1D" ) );
        var array2D = GetMock( compilation, GetFieldType( compilation, "Holder", "Array2D" ) );
        var constructed = GetMock( compilation, GetFieldType( compilation, "Holder", "ConstructedGeneric" ) );
        var definition = GetMock( compilation, GetType( compilation, "Generic" ) );

        var ids = new[] { simple.TypeId.Id, array.TypeId.Id, array2D.TypeId.Id, constructed.TypeId.Id, definition.TypeId.Id };

        // Any two of these collapsing onto one id would silently serve one type for another out of the factory cache.
        Assert.Equal( ids.Length, ids.Distinct( StringComparer.Ordinal ).Count() );
    }

    [Fact]
    public void ToRef_RoundTripsThroughTheTypeId()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );
        var type = GetType( compilation, "SimpleClass" );

        var mock = GetMock( compilation, type );

        // A mock holds no reference: the ref is materialized from the id for the callers that do have a compilation.
        var resolved = mock.ToRef().GetTarget( compilation );

        Assert.Equal( type, resolved );
    }

    [Fact]
    public void GetTypeCode_IsObject_AndDoesNotThrow()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        // Type.GetTypeCode calls GetTypeCodeImpl, whose default reads UnderlyingSystemType -- which a mock cannot
        // provide. GetIntrinsicType calls it for every type, so this must not throw.
        Assert.Equal( TypeCode.Object, Type.GetTypeCode( GetMock( compilation, GetType( compilation, "SimpleClass" ) ) ) );
    }

    [Fact]
    public void UnsupportedMembers_StillThrow()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );
        var mock = GetMock( compilation, GetType( compilation, "SimpleClass" ) );

        // The hierarchy answers structural questions only. Anything requiring a loaded assembly must still fail loudly
        // rather than return something plausible.
        Assert.ThrowsAny<Exception>( () => mock.Assembly );
        Assert.ThrowsAny<Exception>( () => mock.BaseType );
        Assert.ThrowsAny<Exception>( () => mock.GetMethods() );
    }

    [Fact]
    public void UnderlyingSystemType_IsSelf()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );
        var mock = GetMock( compilation, GetType( compilation, "SimpleClass" ) );

        // A mock *is* the type it stands for. This must not throw: RuntimeType.IsAssignableFrom reads it, and
        // GetIntrinsicType calls `typeof(Type).IsAssignableFrom(type)` on every type it writes.
        Assert.Same( mock, mock.UnderlyingSystemType );
        Assert.False( typeof(Type).IsAssignableFrom( mock ) );
    }

    [Fact]
    public void GetElementType_OnNamedType_Throws()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateCompilation( testContext );

        Assert.ThrowsAny<Exception>( () => GetMock( compilation, GetType( compilation, "SimpleClass" ) ).GetElementType() );
    }
}
