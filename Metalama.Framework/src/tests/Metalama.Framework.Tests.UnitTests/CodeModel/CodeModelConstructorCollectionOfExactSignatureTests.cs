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
        public void Throws_ForByRefReflectionType()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
    public C(int x) {}
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.ElementAt( 0 );

            Assert.Throws<ArgumentException>( () => type.Constructors.OfExactSignature( new[] { typeof(int).MakeByRefType() } ) );
        }
    }
}
