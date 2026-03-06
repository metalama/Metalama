// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel
{
    public sealed class CodeModelIAttributeCollectionOfUnboundGenericTypeTests : UnitTestClass
    {
        [Fact]
        public void OfAttributeType_UnboundGenericType_ReturnsAttributes()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class A<T> : System.Attribute
{
}

[A<int>]
class C
{
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var attributeType = compilation.Types.Single( t => t.Name == "A" );
            var type = compilation.Types.Single( t => t.Name == "C" );

            // When passing the unbound generic type (type definition) without explicit ConversionKind,
            // OfAttributeType should still return attributes of constructed instances.
            var attributes = type.Attributes.OfAttributeType( attributeType )
                .ToArray();

            Assert.Single( attributes );
        }

        [Fact]
        public void OfAttributeType_UnboundGenericType_MultipleConstructions()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
class A<T> : System.Attribute
{
}

[A<int>]
[A<string>]
class C
{
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var attributeType = compilation.Types.Single( t => t.Name == "A" );
            var type = compilation.Types.Single( t => t.Name == "C" );

            // When passing the unbound generic type, all constructions should be returned.
            var attributes = type.Attributes.OfAttributeType( attributeType )
                .ToArray();

            Assert.Equal( 2, attributes.Length );
        }

        [Fact]
        public void OfAttributeType_UnboundGenericType_DerivedAttribute()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class A<T> : System.Attribute
{
}

class B : A<int>
{
}

[B]
class C
{
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var attributeType = compilation.Types.Single( t => t.Name == "A" );
            var type = compilation.Types.Single( t => t.Name == "C" );

            // When passing the unbound generic type, attributes of derived types should also be returned.
            var attributes = type.Attributes.OfAttributeType( attributeType )
                .ToArray();

            Assert.Single( attributes );
            Assert.Equal( "B", attributes[0].Type.Name );
        }

        [Fact]
        public void Any_UnboundGenericType_ReturnsTrue()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class A<T> : System.Attribute
{
}

[A<int>]
class C
{
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var attributeType = compilation.Types.Single( t => t.Name == "A" );
            var type = compilation.Types.Single( t => t.Name == "C" );

            // Any should also work with unbound generic types.
            Assert.True( type.Attributes.Any( attributeType ) );
        }

        [Fact]
        public void OfAttributeType_ReflectionType_UnboundGeneric()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
[System.ObsoleteAttribute]
class C
{
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.Single( t => t.Name == "C" );

            // Non-generic reflection type should work as before.
            var attributes = type.Attributes.OfAttributeType( typeof(System.ObsoleteAttribute) )
                .ToArray();

            Assert.Single( attributes );
        }

        [Fact]
        public void OfAttributeType_NonGenericType_StillWorks()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class A : System.Attribute
{
}

[A]
class C
{
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var attributeType = compilation.Types.Single( t => t.Name == "A" );
            var type = compilation.Types.Single( t => t.Name == "C" );

            // Non-generic types should continue to work as before.
            var attributes = type.Attributes.OfAttributeType( attributeType )
                .ToArray();

            Assert.Single( attributes );
        }
    }
}
