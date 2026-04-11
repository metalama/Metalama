// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel
{
    public sealed class CodeModelConstructorCollectionOfExactSignatureTests : UnitTestClass
    {
        [Fact]
        public void Matches_ParameterReflectionType()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
    public C()
    {
    }

    public C(int x)
    {
    }

    public C(string x)
    {
    }
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.ElementAt( 0 );

            var matchedCtor1 = type.Constructors.OfExactSignature( new[] { typeof(int) } );
            Assert.Same( type.Constructors.ElementAt( 1 ), matchedCtor1 );
            var matchedCtor2 = type.Constructors.OfExactSignature( new[] { typeof(string) } );
            Assert.Same( type.Constructors.ElementAt( 2 ), matchedCtor2 );
            var matchedCtor3 = type.Constructors.OfExactSignature( Array.Empty<Type>() );
            Assert.Same( type.Constructors.ElementAt( 0 ), matchedCtor3 );
            var matchedCtor4 = type.Constructors.OfExactSignature( new[] { typeof(double) } );
            Assert.Null( matchedCtor4 );
        }

        [Fact]
        public void ByRefReflectionType_DoesNotMatch()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
    public C(int x) {}
    public C(in int y) {}
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.ElementAt( 0 );

            // By-ref reflection types do not match any parameter.
            Assert.Null( type.Constructors.OfExactSignature( new[] { typeof(int).MakeByRefType() } ) );
        }

        [Fact]
        public void Matches_ImplicitDefaultConstructor()
        {
            using var testContext = this.CreateTestContext();

            // A class with no explicit constructors has an implicit public parameterless constructor.
            // OfExactSignature([]) must match that implicit constructor — this is the lookup used by
            // BaseConstructorResolver.GetImplicitBaseConstructor when resolving the implicit :base(...)
            // call of a default-shape derived constructor.
            const string code = @"
class C
{
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.ElementAt( 0 );

            Assert.Single( type.Constructors );
            var implicitCtor = type.Constructors.Single();
            Assert.True( implicitCtor.IsImplicitlyDeclared );
            Assert.Empty( implicitCtor.Parameters );

            var matchedCtor = type.Constructors.OfExactSignature( Array.Empty<Type>() );
            Assert.Same( implicitCtor, matchedCtor );
        }

        [Fact]
        public void NonByRefReflectionType_MatchesPlainAndIn()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
    public C(int x) {}
    public C(in string y) {}
    public C(ref double z) {}
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.ElementAt( 0 );

            // Non-by-ref reflection types match plain and 'in' parameters.
            Assert.NotNull( type.Constructors.OfExactSignature( new[] { typeof(int) } ) );
            Assert.NotNull( type.Constructors.OfExactSignature( new[] { typeof(string) } ) );

            // Non-by-ref reflection types do not match 'ref' parameters.
            Assert.Null( type.Constructors.OfExactSignature( new[] { typeof(double) } ) );
        }
    }
}
