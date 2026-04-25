// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Comparers;
using Metalama.Testing.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using Xunit;

// ReSharper disable InconsistentNaming

namespace Metalama.Framework.Tests.UnitTests.CodeModel
{
    public sealed class DeclarationComparerTests : UnitTestClass
    {
        [Theory]
        [InlineData( false )]
        [InlineData( true )]
        public void ConversionKindDefault( bool bypassSymbols )
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class A {}

interface I {}

class B : A, I
{
    public static implicit operator int(B a) => 42;
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var typeA = compilation.Types.OfName( "A" ).Single();
            var typeB = compilation.Types.OfName( "B" ).Single();
            var typeI = compilation.Types.OfName( "I" ).Single();

            var comparer = (DeclarationEqualityComparer) compilation.CompilationContext.Comparers.Default;

            // ReSharper disable RedundantArgumentDefaultValue
            Assert.False( comparer.IsConvertibleTo( typeA, typeof(int), ConversionKind.Default, bypassSymbols ) );
            Assert.False( comparer.IsConvertibleTo( typeA, typeof(bool), ConversionKind.Default, bypassSymbols ) );
            Assert.False( comparer.IsConvertibleTo( typeB, typeof(int), ConversionKind.Default, bypassSymbols ) );

            Assert.False(
                comparer.IsConvertibleTo( compilation.Factory.GetTypeByReflectionType( typeof(int) ), typeB, ConversionKind.Default, bypassSymbols ) );

            Assert.False( comparer.IsConvertibleTo( typeA, typeB, ConversionKind.Default, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeB, typeA, ConversionKind.Default, bypassSymbols ) );
            Assert.False( comparer.IsConvertibleTo( typeI, typeB, ConversionKind.Default, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeB, typeI, ConversionKind.Default, bypassSymbols ) );

            Assert.True(
                comparer.IsConvertibleTo(
                    compilation.Factory.GetTypeByReflectionType( typeof(int) ),
                    typeof(object),
                    ConversionKind.Default,
                    bypassSymbols ) );

            if ( !bypassSymbols )
            {
                // Built-in implicit numeric conversions are not supported in bypassSymbols mode.

                Assert.False(
                    comparer.IsConvertibleTo(
                        compilation.Factory.GetTypeByReflectionType( typeof(int) ),
                        typeof(long),
                        ConversionKind.Default ) );

                Assert.False(
                    comparer.IsConvertibleTo(
                        compilation.Factory.GetTypeByReflectionType( typeof(long) ),
                        typeof(int),
                        ConversionKind.Default ) );
            }

            // ReSharper restore RedundantArgumentDefaultValue
        }

        [Theory]
        [InlineData( false )]
        [InlineData( true )]
        public void ConversionKindReference( bool bypassSymbols )
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class A {}

interface I {}

class B : A, I
{
    public static implicit operator int(B a) => 42;
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var typeA = compilation.Types.OfName( "A" ).Single();
            var typeB = compilation.Types.OfName( "B" ).Single();
            var typeI = compilation.Types.OfName( "I" ).Single();

            var comparer = (DeclarationEqualityComparer) compilation.CompilationContext.Comparers.Default;

            Assert.False( comparer.IsConvertibleTo( typeA, typeof(int), ConversionKind.Reference, bypassSymbols ) );
            Assert.False( comparer.IsConvertibleTo( typeA, typeof(bool), ConversionKind.Reference, bypassSymbols ) );
            Assert.False( comparer.IsConvertibleTo( typeB, typeof(int), ConversionKind.Reference, bypassSymbols ) );

            Assert.False(
                comparer.IsConvertibleTo( compilation.Factory.GetTypeByReflectionType( typeof(int) ), typeB, ConversionKind.Reference, bypassSymbols ) );

            Assert.False( comparer.IsConvertibleTo( typeA, typeB, ConversionKind.Reference, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeB, typeA, ConversionKind.Reference, bypassSymbols ) );
            Assert.False( comparer.IsConvertibleTo( typeI, typeB, ConversionKind.Reference, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeB, typeI, ConversionKind.Reference, bypassSymbols ) );

            Assert.False(
                comparer.IsConvertibleTo(
                    compilation.Factory.GetTypeByReflectionType( typeof(int) ),
                    typeof(object),
                    ConversionKind.Reference,
                    bypassSymbols ) );

            Assert.False(
                comparer.IsConvertibleTo( compilation.Factory.GetTypeByReflectionType( typeof(int) ), typeof(long), ConversionKind.Reference, bypassSymbols ) );

            Assert.False(
                comparer.IsConvertibleTo( compilation.Factory.GetTypeByReflectionType( typeof(long) ), typeof(int), ConversionKind.Reference, bypassSymbols ) );
        }

        [Theory]
        [InlineData( false )]
        [InlineData( true )]
        public void ConversionKindImplicit( bool bypassSymbols )
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class A {}

interface I {}

class B : A, I
{
    public static implicit operator int(B a) => 42;
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var typeA = compilation.Types.OfName( "A" ).Single();
            var typeB = compilation.Types.OfName( "B" ).Single();
            var typeI = compilation.Types.OfName( "I" ).Single();

            var comparer = (DeclarationEqualityComparer) compilation.CompilationContext.Comparers.Default;

            Assert.False( comparer.IsConvertibleTo( typeA, typeof(int), ConversionKind.Implicit, bypassSymbols ) );
            Assert.False( comparer.IsConvertibleTo( typeA, typeof(bool), ConversionKind.Implicit, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeB, typeof(int), ConversionKind.Implicit, bypassSymbols ) );

            Assert.False(
                comparer.IsConvertibleTo( compilation.Factory.GetTypeByReflectionType( typeof(int) ), typeB, ConversionKind.Implicit, bypassSymbols ) );

            Assert.False( comparer.IsConvertibleTo( typeA, typeB, ConversionKind.Implicit, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeB, typeA, ConversionKind.Implicit, bypassSymbols ) );
            Assert.False( comparer.IsConvertibleTo( typeI, typeB, ConversionKind.Implicit, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeB, typeI, ConversionKind.Implicit, bypassSymbols ) );

            Assert.True(
                comparer.IsConvertibleTo(
                    compilation.Factory.GetTypeByReflectionType( typeof(int) ),
                    typeof(object),
                    ConversionKind.Implicit,
                    bypassSymbols ) );

            if ( !bypassSymbols )
            {
                // Built-in implicit numeric conversions are not supported in bypassSymbols mode.

                Assert.True(
                    comparer.IsConvertibleTo(
                        compilation.Factory.GetTypeByReflectionType( typeof(int) ),
                        typeof(long),
                        ConversionKind.Implicit,
                        bypassSymbols ) );

                Assert.False(
                    comparer.IsConvertibleTo(
                        compilation.Factory.GetTypeByReflectionType( typeof(long) ),
                        typeof(int),
                        ConversionKind.Implicit,
                        bypassSymbols ) );
            }
        }

        [Theory]
        [InlineData( false )]
        [InlineData( true )]
        public void ConversionKindTypeDefinition( bool bypassSymbols )
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class A<T> : I1 {}

interface I1 {}

interface I2<T> {}

class B<T> : I2<T> {}

class C<T> : A<T> {}

class D : B<int> {}

class E : C<int> {}

class F<T> where T : struct {}

enum G {}

class Instances
{
    public A<int> A;
    public I2<int> I2;
    public F<int> F;
    public F<G> FG;
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var typeA = compilation.Types.OfName( "A" ).Single();
            var typeB = compilation.Types.OfName( "B" ).Single();
            var typeC = compilation.Types.OfName( "C" ).Single();
            var typeD = compilation.Types.OfName( "D" ).Single();
            var typeE = compilation.Types.OfName( "E" ).Single();
            var typeF = compilation.Types.OfName( "F" ).Single();
            var typeI1 = compilation.Types.OfName( "I1" ).Single();
            var typeI2 = compilation.Types.OfName( "I2" ).Single();
            var typeInstanceA = compilation.Types.OfName( "Instances" ).Single().Fields.OfName( "A" ).Single().Type;
            var typeInstanceI2 = compilation.Types.OfName( "Instances" ).Single().Fields.OfName( "I2" ).Single().Type;
            var typeInstanceF = compilation.Types.OfName( "Instances" ).Single().Fields.OfName( "F" ).Single().Type;
            var typeInstanceFG = compilation.Types.OfName( "Instances" ).Single().Fields.OfName( "FG" ).Single().Type;

            var comparer = (DeclarationEqualityComparer) compilation.CompilationContext.Comparers.Default;

            Assert.False( comparer.IsConvertibleTo( typeD, typeA, ConversionKind.TypeDefinition, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeD, typeB, ConversionKind.TypeDefinition, bypassSymbols ) );
            Assert.False( comparer.IsConvertibleTo( typeD, typeC, ConversionKind.TypeDefinition, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeD, typeD, ConversionKind.TypeDefinition, bypassSymbols ) );
            Assert.False( comparer.IsConvertibleTo( typeD, typeI1, ConversionKind.TypeDefinition, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeD, typeI2, ConversionKind.TypeDefinition, bypassSymbols ) );

            Assert.True( comparer.IsConvertibleTo( typeE, typeA, ConversionKind.TypeDefinition, bypassSymbols ) );
            Assert.False( comparer.IsConvertibleTo( typeE, typeB, ConversionKind.TypeDefinition, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeE, typeC, ConversionKind.TypeDefinition, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeE, typeE, ConversionKind.TypeDefinition, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeE, typeI1, ConversionKind.TypeDefinition, bypassSymbols ) );
            Assert.False( comparer.IsConvertibleTo( typeE, typeI2, ConversionKind.TypeDefinition, bypassSymbols ) );

            Assert.True( comparer.IsConvertibleTo( typeInstanceA, typeA, ConversionKind.TypeDefinition, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeInstanceI2, typeI2, ConversionKind.TypeDefinition, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeInstanceF, typeF, ConversionKind.TypeDefinition, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeInstanceFG, typeF, ConversionKind.TypeDefinition, bypassSymbols ) );
        }

        [Theory]
        [InlineData( false )]
        [InlineData( true )]
        public void TypeDefinition_TypeParameter_SimpleInterface( bool bypassSymbols )
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System.Collections.Generic;

interface I2<T> {}

class C<T> where T : IList<int> {}
class D<T> where T : IEnumerable<int> {}
";

            var compilation = testContext.CreateCompilationModel( code );
            var typeI2 = compilation.Types.OfName( "I2" ).Single();
            var comparer = (DeclarationEqualityComparer) compilation.CompilationContext.Comparers.Default;

            // T : IList<int> should match IList<> (TypeDefinition).
            var typeParamC = compilation.Types.OfName( "C" ).Single().TypeParameters[0];
            var listType = compilation.Factory.GetTypeByReflectionType( typeof(IList<>) );

            Assert.True( comparer.IsConvertibleTo( typeParamC, listType, ConversionKind.TypeDefinition, bypassSymbols ) );

            // T : IList<int> should also match IEnumerable<> (transitive via IList<int> : IEnumerable<int>).
            var enumerableType = compilation.Factory.GetTypeByReflectionType( typeof(IEnumerable<>) );
            Assert.True( comparer.IsConvertibleTo( typeParamC, enumerableType, ConversionKind.TypeDefinition, bypassSymbols ) );

            // T : IList<int> should NOT match I2<> (unrelated interface).
            Assert.False( comparer.IsConvertibleTo( typeParamC, typeI2, ConversionKind.TypeDefinition, bypassSymbols ) );

            // T : IEnumerable<int> should NOT match IList<> (IEnumerable doesn't extend IList).
            var typeParamD = compilation.Types.OfName( "D" ).Single().TypeParameters[0];
            Assert.False( comparer.IsConvertibleTo( typeParamD, listType, ConversionKind.TypeDefinition, bypassSymbols ) );
        }

        [Theory]
        [InlineData( false )]
        [InlineData( true )]
        public void TypeDefinition_TypeParameter_GenericTypeConstraint( bool bypassSymbols )
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System.Collections.Generic;

class C<T> where T : IList<int> {}
class D<T> where T : IList<string> {}
";

            var compilation = testContext.CreateCompilationModel( code );
            var comparer = (DeclarationEqualityComparer) compilation.CompilationContext.Comparers.Default;

            var listType = compilation.Factory.GetTypeByReflectionType( typeof(IList<>) );

            // Both T : IList<int> and T : IList<string> should match IList<> (TypeDefinition ignores type args).
            var typeParamC = compilation.Types.OfName( "C" ).Single().TypeParameters[0];
            var typeParamD = compilation.Types.OfName( "D" ).Single().TypeParameters[0];

            Assert.True( comparer.IsConvertibleTo( typeParamC, listType, ConversionKind.TypeDefinition, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeParamD, listType, ConversionKind.TypeDefinition, bypassSymbols ) );
        }

        [Theory]
        [InlineData( false )]
        [InlineData( true )]
        public void TypeDefinition_TypeParameter_RecursiveConstraint( bool bypassSymbols )
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System.Collections.Generic;

class C<T> where T : IList<T> {}
";

            var compilation = testContext.CreateCompilationModel( code );
            var comparer = (DeclarationEqualityComparer) compilation.CompilationContext.Comparers.Default;

            var listType = compilation.Factory.GetTypeByReflectionType( typeof(IList<>) );

            // T : IList<T> should match IList<> and not cause infinite recursion.
            var typeParamC = compilation.Types.OfName( "C" ).Single().TypeParameters[0];
            Assert.True( comparer.IsConvertibleTo( typeParamC, listType, ConversionKind.TypeDefinition, bypassSymbols ) );

            // T : IList<T> should also match IEnumerable<> transitively.
            var enumerableType = compilation.Factory.GetTypeByReflectionType( typeof(IEnumerable<>) );
            Assert.True( comparer.IsConvertibleTo( typeParamC, enumerableType, ConversionKind.TypeDefinition, bypassSymbols ) );
        }

        [Theory]
        [InlineData( false )]
        [InlineData( true )]
        public void TypeDefinition_TypeParameter_TransitiveConstraint( bool bypassSymbols )
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System.Collections.Generic;

class C<T, U> where T : U where U : IList<int> {}
";

            var compilation = testContext.CreateCompilationModel( code );
            var comparer = (DeclarationEqualityComparer) compilation.CompilationContext.Comparers.Default;

            var listType = compilation.Factory.GetTypeByReflectionType( typeof(IList<>) );

            // T : U where U : IList<int> should transitively match IList<>.
            var typeParamT = compilation.Types.OfName( "C" ).Single().TypeParameters[0];
            Assert.True( comparer.IsConvertibleTo( typeParamT, listType, ConversionKind.TypeDefinition, bypassSymbols ) );
        }

        [Theory]
        [InlineData( false )]
        [InlineData( true )]
        public void Reference_TypeParameter_MatchesExactGenericArgs( bool bypassSymbols )
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System.Collections.Generic;

// 'class' constraint ensures T is known to be a reference type,
// so the conversion to its constraint interface is a reference conversion (not boxing).
class C<T> where T : class, IList<int> {}
class D<T> where T : IList<int> {} // No class constraint — conversion is boxing, not reference.

class Instances { public IList<int> ListInt; public IList<string> ListString; }
";

            var compilation = testContext.CreateCompilationModel( code );
            var comparer = (DeclarationEqualityComparer) compilation.CompilationContext.Comparers.Default;

            var typeParamC = compilation.Types.OfName( "C" ).Single().TypeParameters[0];
            var typeParamD = compilation.Types.OfName( "D" ).Single().TypeParameters[0];
            var instanceType = compilation.Types.OfName( "Instances" ).Single();
            var listIntType = instanceType.Fields.OfName( "ListInt" ).Single().Type;
            var listStringType = instanceType.Fields.OfName( "ListString" ).Single().Type;

            // T : class, IList<int> should be Reference-convertible to IList<int>.
            Assert.True( comparer.IsConvertibleTo( typeParamC, listIntType, ConversionKind.Reference, bypassSymbols ) );

            // T : class, IList<int> should NOT be Reference-convertible to IList<string> (IList is invariant).
            Assert.False( comparer.IsConvertibleTo( typeParamC, listStringType, ConversionKind.Reference, bypassSymbols ) );

            // T : IList<int> (no class constraint) — boxing, not reference.
            Assert.False( comparer.IsConvertibleTo( typeParamD, listIntType, ConversionKind.Reference, bypassSymbols ) );
        }

        [Theory]
        [InlineData( false )]
        [InlineData( true )]
        public void TypeDefinition_TypeParameter_Unconstrained( bool bypassSymbols )
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System.Collections.Generic;

class C<T> {}
";

            var compilation = testContext.CreateCompilationModel( code );
            var comparer = (DeclarationEqualityComparer) compilation.CompilationContext.Comparers.Default;

            var listType = compilation.Factory.GetTypeByReflectionType( typeof(IList<>) );

            // Unconstrained T should NOT match IList<>.
            var typeParamC = compilation.Types.OfName( "C" ).Single().TypeParameters[0];
            Assert.False( comparer.IsConvertibleTo( typeParamC, listType, ConversionKind.TypeDefinition, bypassSymbols ) );
        }

        [Theory]
        [InlineData( false )]
        [InlineData( true )]
        public void ConversionKindIdentical( bool bypassSymbols )
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class A {}

interface I {}

class B : A, I
{
    public static implicit operator int(B a) => 42;
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var typeA = compilation.Types.OfName( "A" ).Single();
            var typeB = compilation.Types.OfName( "B" ).Single();
            var typeI = compilation.Types.OfName( "I" ).Single();

            var comparer = (DeclarationEqualityComparer) compilation.CompilationContext.Comparers.Default;

            // Identical requires exact type equality.
            Assert.True( comparer.IsConvertibleTo( typeA, typeA, ConversionKind.Identical, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeB, typeB, ConversionKind.Identical, bypassSymbols ) );
            Assert.True( comparer.IsConvertibleTo( typeI, typeI, ConversionKind.Identical, bypassSymbols ) );

            // Derived/base types are NOT identical.
            Assert.False( comparer.IsConvertibleTo( typeA, typeB, ConversionKind.Identical, bypassSymbols ) );
            Assert.False( comparer.IsConvertibleTo( typeB, typeA, ConversionKind.Identical, bypassSymbols ) );

            // Interface implementations are NOT identical.
            Assert.False( comparer.IsConvertibleTo( typeB, typeI, ConversionKind.Identical, bypassSymbols ) );
            Assert.False( comparer.IsConvertibleTo( typeI, typeB, ConversionKind.Identical, bypassSymbols ) );

            // int to object is NOT identical (even though it's a boxing conversion).
            Assert.False(
                comparer.IsConvertibleTo(
                    compilation.Factory.GetTypeByReflectionType( typeof(int) ),
                    typeof(object),
                    ConversionKind.Identical,
                    bypassSymbols ) );

            // int to int IS identical.
            Assert.True(
                comparer.IsConvertibleTo(
                    compilation.Factory.GetTypeByReflectionType( typeof(int) ),
                    typeof(int),
                    ConversionKind.Identical,
                    bypassSymbols ) );

            // B to int is NOT identical (even though there's an implicit operator).
            Assert.False( comparer.IsConvertibleTo( typeB, typeof(int), ConversionKind.Identical, bypassSymbols ) );
        }
    }
}