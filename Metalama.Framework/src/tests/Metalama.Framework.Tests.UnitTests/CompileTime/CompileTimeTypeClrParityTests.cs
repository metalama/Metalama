// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

/// <summary>
/// Verifies that a <see cref="CompileTimeType"/> answers each of its <em>implemented</em> members exactly as the CLR
/// <see cref="Type"/> for the same type would.
/// </summary>
/// <remarks>
/// Three families of members are deliberately <em>not</em> expected to match the CLR and are covered separately:
/// <list type="bullet">
/// <item>members that a mock cannot answer (assembly, base type, members, attributes, …) throw — see
/// <see cref="UnsupportedMembers_Throw"/>;</item>
/// <item>equality is by type id, not by CLR reference identity — see <see cref="Equality_IsByTypeId"/>;</item>
/// <item><see cref="Type.FullName"/> of a <em>constructed generic</em> is the simplified, assembly-neutral form the
/// serializer needs, not the CLR's assembly-qualified form — see <see cref="FullName_OfConstructedGeneric_IsAssemblyNeutral"/>;
/// and <see cref="Type.GetTypeCode(Type)"/> / <see cref="Type.UnderlyingSystemType"/> are documented deviations — see
/// <see cref="GetTypeCode_IsAlwaysObject"/> and <see cref="UnderlyingSystemType_IsSelf"/>.</item>
/// </list>
/// </remarks>
public sealed class CompileTimeTypeClrParityTests : UnitTestClass
{
    // Types with no constructed-generic component anywhere, for which even FullName matches the CLR exactly.
    public static IEnumerable<object[]> FullyMatchingTypes =>
    [
        [typeof(int)], [typeof(string)], [typeof(object)], [typeof(DayOfWeek)], [typeof(Guid)],
        [typeof(int[])], [typeof(int[,])], [typeof(int[,,])], [typeof(int[][])], [typeof(string[])],
        [typeof(DayOfWeek[])], [typeof(Environment.SpecialFolder)], [typeof(DateTimeKind)]
    ];

    // Additionally covers constructed generics (where FullName is assembly-neutral and so excluded from the FullName check).
    public static IEnumerable<object[]> AllTypes =>
        FullyMatchingTypes.Concat(
        [
            [typeof(List<int>)], [typeof(List<string>)], [typeof(Dictionary<string, int>)], [typeof(List<int>[])],
            [typeof(List<List<int>>)], [typeof(KeyValuePair<string, int>)], [typeof(int?)]
        ] );

    private static CompileTimeType Mock( CompilationModel compilation, Type type )
        => compilation.CompilationContext.CompileTimeTypeFactory.Get(
            compilation.Factory.GetTypeByReflectionType( type ).GetSymbol().AssertSymbolNotNull() );

    [Theory]
    [MemberData( nameof(AllTypes) )]
    public void Names_MatchClr( Type clrType )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );
        var mock = Mock( compilation, clrType );

        Assert.Equal( clrType.Name, mock.Name );
        Assert.Equal( clrType.Namespace, mock.Namespace );
        Assert.Equal( clrType.ToString(), mock.ToString() );
    }

    [Theory]
    [MemberData( nameof(FullyMatchingTypes) )]
    public void FullName_MatchesClr( Type clrType )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );

        Assert.Equal( clrType.FullName, Mock( compilation, clrType ).FullName );
    }

    [Theory]
    [MemberData( nameof(AllTypes) )]
    public void ShapeFlags_MatchClr( Type clrType )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );
        var mock = Mock( compilation, clrType );

        Assert.Equal( clrType.IsArray, mock.IsArray );
        Assert.Equal( clrType.IsPointer, mock.IsPointer );
        Assert.Equal( clrType.HasElementType, mock.HasElementType );
        Assert.Equal( clrType.IsByRef, mock.IsByRef );
        Assert.Equal( clrType.IsEnum, mock.IsEnum );
        Assert.Equal( clrType.IsValueType, mock.IsValueType );
        Assert.Equal( clrType.IsGenericType, mock.IsGenericType );
        Assert.Equal( clrType.IsGenericTypeDefinition, mock.IsGenericTypeDefinition );
        Assert.Equal( clrType.IsConstructedGenericType, mock.IsConstructedGenericType );
        Assert.Equal( clrType.IsGenericParameter, mock.IsGenericParameter );
        Assert.Equal( clrType.IsNested, mock.IsNested );
        Assert.Equal( clrType.ContainsGenericParameters, mock.ContainsGenericParameters );
    }

    [Theory]
    [MemberData( nameof(AllTypes) )]
    public void ElementType_MatchesClr( Type clrType )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );
        var mock = Mock( compilation, clrType );

        if ( clrType.HasElementType )
        {
            Assert.Equal( clrType.GetElementType()!.Name, mock.GetElementType()!.Name );

            if ( clrType.IsArray )
            {
                Assert.Equal( clrType.GetArrayRank(), mock.GetArrayRank() );
            }
        }
        else
        {
            // Reflection: GetElementType returns null for a type that has no element type (rather than throwing).
            Assert.Null( clrType.GetElementType() );
            Assert.Throws<NotSupportedException>( () => mock.GetElementType() );
        }
    }

    [Theory]
    [MemberData( nameof(AllTypes) )]
    public void GenericStructure_MatchesClr( Type clrType )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );
        var mock = Mock( compilation, clrType );

        // GetGenericArguments is only part of the generic-type contract. For a non-generic type the CLR is inconsistent
        // -- notably an array of a generic (List<int>[]) reports IsGenericType == false yet returns a non-empty argument
        // list; the mock returns empty there, which is the sane reading and consistent with its IsGenericType. We
        // therefore only assert parity for types the CLR itself considers generic.
        if ( clrType.IsGenericType )
        {
            var clrArgs = clrType.GetGenericArguments();
            var mockArgs = mock.GetGenericArguments();

            Assert.Equal( clrArgs.Length, mockArgs.Length );
            Assert.Equal( clrArgs.Select( a => a.Name ), mockArgs.Select( a => a.Name ) );
        }
        else
        {
            Assert.Empty( mock.GetGenericArguments() );
        }

        if ( clrType.IsGenericType && !clrType.IsGenericTypeDefinition )
        {
            Assert.Equal( clrType.GetGenericTypeDefinition().Name, mock.GetGenericTypeDefinition().Name );
            Assert.Equal( clrType.GetGenericTypeDefinition().FullName, mock.GetGenericTypeDefinition().FullName );
        }
    }

    [Theory]
    [MemberData( nameof(AllTypes) )]
    public void DeclaringType_MatchesClr( Type clrType )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );
        var mock = Mock( compilation, clrType );

        Assert.Equal( clrType.DeclaringType?.Name, mock.DeclaringType?.Name );
    }

    [Fact]
    public void MakeArrayType_MatchesClr()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );
        var mock = Mock( compilation, typeof(string) );

        var array = mock.MakeArrayType();
        Assert.True( array.IsArray );
        Assert.Equal( typeof(string[]).Name, array.Name );
        Assert.Equal( typeof(string[]).ToString(), array.ToString() );
        Assert.Equal( 1, array.GetArrayRank() );

        var array2D = mock.MakeArrayType( 2 );
        Assert.Equal( typeof(string[,]).Name, array2D.Name );
        Assert.Equal( 2, array2D.GetArrayRank() );
    }

    [Fact]
    public void MakeArrayType_ThenGetElementType_ReturnsOriginal()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );
        var mock = Mock( compilation, typeof(string) );

        Assert.Equal( mock, mock.MakeArrayType().GetElementType() );
    }

    [Fact]
    public void GetTypeCode_IsAlwaysObject()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );

        // Documented deviation: the CLR returns Int32 for int etc.; a mock cannot, and always answers Object. It must
        // not throw, because GetIntrinsicType (in the serializer) calls it on every type.
        Assert.Equal( TypeCode.Object, Type.GetTypeCode( Mock( compilation, typeof(int) ) ) );
        Assert.Equal( TypeCode.Object, Type.GetTypeCode( Mock( compilation, typeof(string) ) ) );
    }

    [Fact]
    public void UnderlyingSystemType_IsSelf()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );
        var mock = Mock( compilation, typeof(string) );

        // Documented deviation. Must not throw: RuntimeType.IsAssignableFrom reads it, and GetIntrinsicType calls
        // typeof(Type).IsAssignableFrom(type) on every type.
        Assert.Same( mock, mock.UnderlyingSystemType );
    }

    [Fact]
    public void FullName_OfConstructedGeneric_IsAssemblyNeutral()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );
        var mock = Mock( compilation, typeof(List<int>) );

        // The CLR assembly-qualifies the arguments (List`1[[System.Int32, System.Private.CoreLib, Version=...]]). The
        // mock deliberately uses the assembly-neutral form, because the serialized id must be version-independent.
        Assert.Equal( "System.Collections.Generic.List`1[System.Int32]", mock.FullName );
        Assert.Contains( "System.Private.CoreLib", typeof(List<int>).FullName!, StringComparison.Ordinal );
        Assert.DoesNotContain( "System.Private.CoreLib", mock.FullName, StringComparison.Ordinal );
    }

    [Fact]
    public void Equality_IsByTypeId()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );

        var a = Mock( compilation, typeof(List<int>) );
        var b = Mock( compilation, typeof(List<int>) );

        // Not CLR reference identity: two mocks of the same type are equal, and equal to a freshly-made generic.
        Assert.Equal( a, b );
        Assert.Equal( a.GetHashCode(), b.GetHashCode() );
    }

    [Theory]
    [MemberData( nameof(AllTypes) )]
    public void UnsupportedMembers_Throw( Type clrType )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );
        var mock = Mock( compilation, clrType );

        // Everything that requires a loaded assembly or reflected member must fail loudly rather than mislead.
        Assert.ThrowsAny<Exception>( () => mock.Assembly );
        Assert.ThrowsAny<Exception>( () => mock.AssemblyQualifiedName );
        Assert.ThrowsAny<Exception>( () => mock.BaseType );
        Assert.ThrowsAny<Exception>( () => mock.GUID );
        Assert.ThrowsAny<Exception>( () => mock.Module );
        Assert.ThrowsAny<Exception>( () => mock.GetMethods() );
        Assert.ThrowsAny<Exception>( () => mock.GetFields() );
        Assert.ThrowsAny<Exception>( () => mock.GetConstructors() );
        Assert.ThrowsAny<Exception>( () => mock.GetProperties() );
        Assert.ThrowsAny<Exception>( () => mock.GetEvents() );
        Assert.ThrowsAny<Exception>( () => mock.GetMembers() );
        Assert.ThrowsAny<Exception>( () => mock.GetInterfaces() );
        Assert.ThrowsAny<Exception>( () => mock.GetNestedTypes() );
        Assert.ThrowsAny<Exception>( () => mock.GetCustomAttributes( false ) );
        Assert.ThrowsAny<Exception>( () => mock.IsDefined( typeof(FlagsAttribute), false ) );

        // These read GetAttributeFlagsImpl, which a mock cannot provide.
        Assert.ThrowsAny<Exception>( () => mock.IsAbstract );
        Assert.ThrowsAny<Exception>( () => mock.IsSealed );
        Assert.ThrowsAny<Exception>( () => mock.IsInterface );
        Assert.ThrowsAny<Exception>( () => mock.IsPublic );
    }
}
