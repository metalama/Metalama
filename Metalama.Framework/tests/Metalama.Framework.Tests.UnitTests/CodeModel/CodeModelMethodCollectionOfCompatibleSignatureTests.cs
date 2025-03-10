// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Tests.UnitTests.Utilities;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using Xunit;

// ReSharper disable StringLiteralTypo

namespace Metalama.Framework.Tests.UnitTests.CodeModel
{
    public sealed class CodeModelMethodCollectionOfCompatibleSignatureTests : UnitTestClass
    {
        [Fact]
        public void Matches_ParameterTypes()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
    public void Foo()
    {
    }

    public void Foo(int x)
    {
    }

    public void Foo(object x)
    {
    }

    public void Foo(int x, int y)
    {
    }

    public void Foo(object x, object y)
    {
    }
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.ElementAt( 0 );
            var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );
            var objectType = compilation.Factory.GetTypeByReflectionType( typeof(object) );

            var matchedMethods1 = type.Methods.OfCompatibleSignature( "Foo", argumentTypes: Array.Empty<IType>() );
            Assert.Equal( new[] { type.Methods.ElementAt( 0 ) }, matchedMethods1 );
            var matchedMethods2 = type.Methods.OfCompatibleSignature( "Foo", argumentTypes: new[] { intType } ).ToArray();
            Assert.Equal( new[] { type.Methods.ElementAt( 1 ), type.Methods.ElementAt( 2 ) }, matchedMethods2 );
            var matchedMethods3 = type.Methods.OfCompatibleSignature( "Foo", argumentTypes: new[] { objectType } ).ToArray();
            Assert.Equal( new[] { type.Methods.ElementAt( 2 ) }, matchedMethods3 );
            var matchedMethods4 = type.Methods.OfCompatibleSignature( "Foo", argumentTypes: new[] { intType, intType } ).ToArray();
            Assert.Equal( new[] { type.Methods.ElementAt( 3 ), type.Methods.ElementAt( 4 ) }, matchedMethods4 );
            var matchedMethods5 = type.Methods.OfCompatibleSignature( "Foo", argumentTypes: new[] { objectType, objectType } ).ToArray();
            Assert.Equal( new[] { type.Methods.ElementAt( 4 ) }, matchedMethods5 );
            var matchedMethods6 = type.Methods.OfCompatibleSignature( "Foo", argumentTypes: new[] { intType, objectType } ).ToArray();
            Assert.Equal( new[] { type.Methods.ElementAt( 4 ) }, matchedMethods6 );
            var matchedMethods7 = type.Methods.OfCompatibleSignature( "Foo", argumentTypes: new[] { objectType, intType } ).ToArray();
            Assert.Equal( new[] { type.Methods.ElementAt( 4 ) }, matchedMethods7 );
            var matchedMethods8 = type.Methods.OfCompatibleSignature( "Foo", argumentTypes: new[] { objectType, objectType, objectType } ).ToArray();
            Assert.Equal( [], matchedMethods8 );
        }

        [Fact]
        public void Matches_ParameterTypeRefKinds()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
    public void Foo(int x)
    {
    }

    public void Foo(ref int x)
    {
    }

    public void Bar(int x)
    {
    }

    public void Bar(out int x)
    {
        x = 0;
    }

    public void Quz(int x)
    {
    }

    public void Quz(in int x)
    {
    }
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.ElementAt( 0 );
            var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );

            var matchedMethods1 = type.Methods.OfCompatibleSignature( "Foo", new[] { intType } );
            Assert.Equal( new[] { type.Methods.ElementAt( 0 ), type.Methods.ElementAt( 1 ) }, matchedMethods1 );
            var matchedMethods2 = type.Methods.OfCompatibleSignature( "Foo", new[] { intType }, new RefKind?[] { null } );
            Assert.Equal( new[] { type.Methods.ElementAt( 0 ), type.Methods.ElementAt( 1 ) }, matchedMethods2 );
            var matchedMethods3 = type.Methods.OfCompatibleSignature( "Foo", new[] { intType }, new RefKind?[] { RefKind.None } );
            Assert.Equal( new[] { type.Methods.ElementAt( 0 ) }, matchedMethods3 );
            var matchedMethods4 = type.Methods.OfCompatibleSignature( "Foo", new[] { intType }, new RefKind?[] { RefKind.Ref } );
            Assert.Equal( new[] { type.Methods.ElementAt( 1 ) }, matchedMethods4 );
            var matchedMethods5 = type.Methods.OfCompatibleSignature( "Foo", new[] { intType }, new RefKind?[] { RefKind.RefReadOnly } );
            Assert.Equal( Array.Empty<IMethod>(), matchedMethods5 );
            var matchedMethods6 = type.Methods.OfCompatibleSignature( "Bar", new[] { intType }, new RefKind?[] { null } );
            Assert.Equal( new[] { type.Methods.ElementAt( 2 ), type.Methods.ElementAt( 3 ) }, matchedMethods6 );
            var matchedMethods7 = type.Methods.OfCompatibleSignature( "Bar", new[] { intType }, new RefKind?[] { RefKind.None } );
            Assert.Equal( new[] { type.Methods.ElementAt( 2 ) }, matchedMethods7 );
            var matchedMethods8 = type.Methods.OfCompatibleSignature( "Bar", new[] { intType }, new RefKind?[] { RefKind.Out } );
            Assert.Equal( new[] { type.Methods.ElementAt( 3 ) }, matchedMethods8 );
            var matchedMethods9 = type.Methods.OfCompatibleSignature( "Quz", new[] { intType }, new RefKind?[] { null } );
            Assert.Equal( new[] { type.Methods.ElementAt( 4 ), type.Methods.ElementAt( 5 ) }, matchedMethods9 );
            var matchedMethods10 = type.Methods.OfCompatibleSignature( "Quz", new[] { intType }, new RefKind?[] { RefKind.None } );
            Assert.Equal( new[] { type.Methods.ElementAt( 4 ) }, matchedMethods10 );
            var matchedMethods11 = type.Methods.OfCompatibleSignature( "Quz", new[] { intType }, new RefKind?[] { RefKind.In } );
            Assert.Equal( new[] { type.Methods.ElementAt( 5 ) }, matchedMethods11 );
        }

        [Fact]
        public void Matches_ParameterReflectionTypes()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
    public void Foo()
    {
    }

    public void Foo(int x)
    {
    }

    public void Foo(object x)
    {
    }

    public void Foo(int x, int y)
    {
    }

    public void Foo(object x, object y)
    {
    }
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.ElementAt( 0 );

            var matchedMethods1 = type.Methods.OfCompatibleSignature( "Foo", Array.Empty<IType>() ).ToArray();
            Assert.Equal( new[] { type.Methods.ElementAt( 0 ) }, matchedMethods1 );
            var matchedMethods2 = type.Methods.OfCompatibleSignature( "Foo", new[] { typeof(int) } ).ToArray();
            Assert.Equal( new[] { type.Methods.ElementAt( 1 ), type.Methods.ElementAt( 2 ) }, matchedMethods2 );
            var matchedMethods3 = type.Methods.OfCompatibleSignature( "Foo", new[] { typeof(object) } ).ToArray();
            Assert.Equal( new[] { type.Methods.ElementAt( 2 ) }, matchedMethods3 );
            var matchedMethods4 = type.Methods.OfCompatibleSignature( "Foo", new[] { typeof(int), typeof(int) } ).ToArray();
            Assert.Equal( new[] { type.Methods.ElementAt( 3 ), type.Methods.ElementAt( 4 ) }, matchedMethods4 );
            var matchedMethods5 = type.Methods.OfCompatibleSignature( "Foo", new[] { typeof(object), typeof(object) } ).ToArray();
            Assert.Equal( new[] { type.Methods.ElementAt( 4 ) }, matchedMethods5 );
        }

        [Fact]
        public void Matches_IsStatic()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
    public void Foo()
    {
    }

    public static void Bar()
    {
    }
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.ElementAt( 0 );

            var matchedMethods1 = type.Methods.OfCompatibleSignature( "Foo", Array.Empty<IType>(), isStatic: false );
            Assert.Equal( new[] { type.Methods.ElementAt( 0 ) }, matchedMethods1 );
            var matchedMethods2 = type.Methods.OfCompatibleSignature( "Foo", Array.Empty<IType>(), isStatic: true );
            Assert.Equal( Array.Empty<IMethod>(), matchedMethods2 );
            var matchedMethods3 = type.Methods.OfCompatibleSignature( "Foo", Array.Empty<IType>(), isStatic: null );
            Assert.Equal( new[] { type.Methods.ElementAt( 0 ) }, matchedMethods3 );
            var matchedMethods4 = type.Methods.OfCompatibleSignature( "Bar", Array.Empty<IType>(), isStatic: false );
            Assert.Equal( Array.Empty<IMethod>(), matchedMethods4 );
            var matchedMethods5 = type.Methods.OfCompatibleSignature( "Bar", Array.Empty<IType>(), isStatic: true );
            Assert.Equal( new[] { type.Methods.ElementAt( 1 ) }, matchedMethods5 );
            var matchedMethods6 = type.Methods.OfCompatibleSignature( "Bar", Array.Empty<IType>(), isStatic: null );
            Assert.Equal( new[] { type.Methods.ElementAt( 1 ) }, matchedMethods6 );
        }

        [Fact]
        public void Matches_Params()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
    public void Foo() // 0
    {
    }

    public void Foo(int x) // 1
    {
    }

    public void Foo(int x, int y) // 2
    {
    }

    public void Foo(object x) // 3
    {
    }

    public void Foo(object x, object y) // 4
    {
    }

    public void Foo( params int[] a) // 5
    {
    }

    public void Foo( params object[] a) // 6
    {
    }

    public void Foo( int x, params object[] a) // 7
    {
    }

    public void Foo( object x, params int[] a) // 8
    {
    }
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.ElementAt( 0 );
            var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );
            var objectType = compilation.Factory.GetTypeByReflectionType( typeof(object) );
            var intArrayType = compilation.Factory.GetTypeByReflectionType( typeof(int[]) );
            var objectArrayType = compilation.Factory.GetTypeByReflectionType( typeof(object[]) );

            var matchedMethods1 = type.Methods.OfCompatibleSignature( "Foo", Array.Empty<IType>() );
            Assert.Equal( new[] { type.Methods.ElementAt( 0 ), type.Methods.ElementAt( 5 ), type.Methods.ElementAt( 6 ) }, matchedMethods1 );
            var matchedMethods2 = type.Methods.OfCompatibleSignature( "Foo", new[] { intType } ).ToArray();

            Assert.Equal(
                new[]
                {
                    type.Methods.ElementAt( 1 ),
                    type.Methods.ElementAt( 3 ),
                    type.Methods.ElementAt( 5 ),
                    type.Methods.ElementAt( 6 ),
                    type.Methods.ElementAt( 7 ),
                    type.Methods.ElementAt( 8 )
                },
                matchedMethods2 );

            var matchedMethods3 = type.Methods.OfCompatibleSignature( "Foo", new[] { intType, intType } ).ToArray();

            Assert.Equal(
                new[]
                {
                    type.Methods.ElementAt( 2 ),
                    type.Methods.ElementAt( 4 ),
                    type.Methods.ElementAt( 5 ),
                    type.Methods.ElementAt( 6 ),
                    type.Methods.ElementAt( 7 ),
                    type.Methods.ElementAt( 8 )
                },
                matchedMethods3 );

            var matchedMethods4 = type.Methods.OfCompatibleSignature( "Foo", new[] { intType, intType, intType } ).ToArray();

            Assert.Equal(
                new[] { type.Methods.ElementAt( 5 ), type.Methods.ElementAt( 6 ), type.Methods.ElementAt( 7 ), type.Methods.ElementAt( 8 ) },
                matchedMethods4 );

            var matchedMethods5 = type.Methods.OfCompatibleSignature( "Foo", new[] { objectType } ).ToArray();
            Assert.Equal( new[] { type.Methods.ElementAt( 3 ), type.Methods.ElementAt( 6 ), type.Methods.ElementAt( 8 ) }, matchedMethods5 );
            var matchedMethods6 = type.Methods.OfCompatibleSignature( "Foo", new[] { objectType, objectType } ).ToArray();
            Assert.Equal( new[] { type.Methods.ElementAt( 4 ), type.Methods.ElementAt( 6 ) }, matchedMethods6 );
            var matchedMethods7 = type.Methods.OfCompatibleSignature( "Foo", new[] { objectType, objectType, objectType } ).ToArray();
            Assert.Equal( new[] { type.Methods.ElementAt( 6 ) }, matchedMethods7 );
            var matchedMethods8 = type.Methods.OfCompatibleSignature( "Foo", new[] { intArrayType } ).ToArray();

            Assert.Equal(
                new[] { type.Methods.ElementAt( 3 ), type.Methods.ElementAt( 5 ), type.Methods.ElementAt( 6 ), type.Methods.ElementAt( 8 ) },
                matchedMethods8 );

            var matchedMethods9 = type.Methods.OfCompatibleSignature( "Foo", new[] { objectArrayType } ).ToArray();
            Assert.Equal( new[] { type.Methods.ElementAt( 3 ), type.Methods.ElementAt( 6 ), type.Methods.ElementAt( 8 ) }, matchedMethods9 );
            var matchedMethods10 = type.Methods.OfCompatibleSignature( "Foo", new[] { intType, objectArrayType } ).ToArray();
            Assert.Equal( new[] { type.Methods.ElementAt( 4 ), type.Methods.ElementAt( 6 ), type.Methods.ElementAt( 7 ) }, matchedMethods10 );
            var matchedMethods11 = type.Methods.OfCompatibleSignature( "Foo", new[] { objectType, intArrayType } ).ToArray();
            Assert.Equal( new[] { type.Methods.ElementAt( 4 ), type.Methods.ElementAt( 6 ), type.Methods.ElementAt( 8 ) }, matchedMethods11 );
        }

        [Fact]
        public void Matches_ParamsCollections()
        {
            using var testContext = this.CreateTestContext();

            const string code = """
                                using System;
                                using System.Collections.Generic;
                                using System.Collections.Immutable;
                                using System.Runtime.CompilerServices;

                                class C
                                {
                                    public void Foo() { } // 0
                                    public void Foo(int x) { } // 1
                                    public void Foo(int x, int y) { } // 2
                                
                                    public void Foo(params int[] a) { } // 3
                                    public void Foo(params List<int> l) { } // 4
                                    public void Foo(params Span<int> s) { } // 5
                                    public void Foo(params ReadOnlySpan<int> s) { } // 6
                                    public void Foo(params ImmutableArray<int> a) { } // 7
                                    public void Foo(params CustomNonEnumerableCollection c) { } // 8
                                }

                                [CollectionBuilder(typeof(CustomNonEnumerableCollection), "Create")]
                                public class CustomNonEnumerableCollection
                                {
                                    public static CustomNonEnumerableCollection Create(ReadOnlySpan<int> s) => null!;
                                
                                    public IEnumerator<int> GetEnumerator() => null!;
                                }

                                namespace System.Runtime.CompilerServices
                                {
                                    class CollectionBuilderAttribute(Type collectionType, string factoryMethod) : Attribute;
                                }
                                """;

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.OfName( "C" ).Single();
            var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );

            IMethod[] paramsMethods =
            [
                type.Methods.ElementAt( 3 ),
                type.Methods.ElementAt( 4 ),
                type.Methods.ElementAt( 5 ),
                type.Methods.ElementAt( 6 ),
                type.Methods.ElementAt( 7 ),
                type.Methods.ElementAt( 8 ),
            ];

            var matchedMethods1 = type.Methods.OfCompatibleSignature( "Foo", Array.Empty<IType>() );
            Assert.Equal( [type.Methods.ElementAt( 0 ), .. paramsMethods], matchedMethods1 );

            var matchedMethods2 = type.Methods.OfCompatibleSignature( "Foo", [intType] );
            Assert.Equal( [type.Methods.ElementAt( 1 ), .. paramsMethods], matchedMethods2 );

            var matchedMethods3 = type.Methods.OfCompatibleSignature( "Foo", [intType, intType] );
            Assert.Equal( [type.Methods.ElementAt( 2 ), .. paramsMethods], matchedMethods3 );
        }

        [Fact]
        public void Matches_InheritanceHierarchy()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class A
{
    public void Foo() {}
    public void Bar() {}
    public virtual void Baz() {}
    public virtual void Qux() {}
}

class B : A
{
    public new void Foo() {}
    public override void Baz() {}
    public void Quz() {}
    public virtual void Qur() {}
    public void Trx() {}
    public virtual void Trw() {}
}

class C : B
{
    public new void Trx() {}
    public override void Trw() {}
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var types = compilation.Types.OrderBySource();
            var typeA = types.ElementAt( 0 );
            var typeAMethods = typeA.Methods.ToOrderedList( m => m.GetPrimaryDeclarationSyntax()!.GetLocation().SourceSpan.Start );

            var typeB = types.ElementAt( 1 );
            var typeBMethods = typeB.Methods.ToOrderedList( m => m.GetPrimaryDeclarationSyntax()!.GetLocation().SourceSpan.Start );

            var typeC = types.ElementAt( 2 );

            var matchedMethods1 = typeC.AllMethods.OfCompatibleSignature( "Foo", Array.Empty<IType>() ).ToArray();

            Assert.Equal( new[] { typeBMethods.ElementAt( 0 ) }, matchedMethods1 );
            var matchedMethods2 = typeC.AllMethods.OfCompatibleSignature( "Bar", Array.Empty<IType>() ).ToArray();

            Assert.Equal( new[] { typeAMethods.ElementAt( 1 ) }, matchedMethods2 );
            var matchedMethods3 = typeC.AllMethods.OfCompatibleSignature( "Baz", Array.Empty<IType>() ).ToArray();
            Assert.Equal( new[] { typeBMethods.ElementAt( 1 ) }, matchedMethods3 );
            var matchedMethods4 = typeC.AllMethods.OfCompatibleSignature( "Qux", Array.Empty<IType>() ).ToArray();
            Assert.Equal( new[] { typeAMethods.ElementAt( 3 ) }, matchedMethods4 );
            var matchedMethods5 = typeC.AllMethods.OfCompatibleSignature( "Quz", Array.Empty<IType>() ).ToArray();
            Assert.Equal( new[] { typeBMethods.ElementAt( 2 ) }, matchedMethods5 );
            var matchedMethods6 = typeC.AllMethods.OfCompatibleSignature( "Qur", Array.Empty<IType>() ).ToArray();
            Assert.Equal( new[] { typeBMethods.ElementAt( 3 ) }, matchedMethods6 );
            var matchedMethods7 = typeC.AllMethods.OfCompatibleSignature( "Trx", Array.Empty<IType>() ).ToArray();
            Assert.Equal( new[] { typeC.Methods.ElementAt( 0 ) }, matchedMethods7 );
            var matchedMethods8 = typeC.AllMethods.OfCompatibleSignature( "Trw", Array.Empty<IType>() ).ToArray();
            Assert.Equal( new[] { typeC.Methods.ElementAt( 1 ) }, matchedMethods8 );
            var matchedMethods9 = typeC.AllMethods.OfCompatibleSignature( "Xyzzy", Array.Empty<IType>() ).ToArray();
            Assert.Equal( [], matchedMethods9 );
        }
    }
}