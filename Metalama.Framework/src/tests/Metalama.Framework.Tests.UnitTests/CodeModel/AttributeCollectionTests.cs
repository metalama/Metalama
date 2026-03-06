// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel
{
    public sealed class AttributeCollectionTests : UnitTestClass
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
        public void OfAttributeType_ReflectionType_NonGeneric()
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
        public void OfAttributeType_AndAny_WithReflectionUnboundGenericType()
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

            // Passing an unbound generic reflection type (List<>) should be accepted and
            // go through the ConversionKind.Default -> TypeDefinition promotion path.
            var unboundGenericReflectionType = typeof(System.Collections.Generic.List<>);

            var attributes = type.Attributes.OfAttributeType( unboundGenericReflectionType )
                .ToArray();

            Assert.Empty( attributes );
            Assert.False( type.Attributes.Any( unboundGenericReflectionType ) );
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

        [Fact]
        public void OfAttributeType_UnboundGenericType_NoMatch()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class A<T> : System.Attribute
{
}

class B : System.Attribute
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

            // Unbound generic type should not match unrelated attributes.
            var attributes = type.Attributes.OfAttributeType( attributeType )
                .ToArray();

            Assert.Empty( attributes );
        }

        [Fact]
        public void OfAttributeType_UnboundGenericType_GenericInterface()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
interface I<T>
{
}

class A : System.Attribute, I<int>
{
}

[A]
class C
{
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var interfaceType = compilation.Types.Single( t => t.Name == "I" );
            var type = compilation.Types.Single( t => t.Name == "C" );

            // Unbound generic interface type should match attributes implementing it.
            var attributes = type.Attributes.OfAttributeType( interfaceType )
                .ToArray();

            Assert.Single( attributes );
            Assert.Equal( "A", attributes[0].Type.Name );
        }

        [Fact]
        public void Any_UnboundGenericType_NoMatch_ReturnsFalse()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class A<T> : System.Attribute
{
}

class B : System.Attribute
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

            // Any with unbound generic type should return false when no match.
            Assert.False( type.Attributes.Any( attributeType ) );
        }

        [Fact]
        public void OfAttributeType_ConstructedGenericType_ExactMatch()
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

            // Passing a constructed generic type (not unbound) should only match that specific construction.
            var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );
            var constructedType = attributeType.MakeGenericInstance( intType );
            var attributes = type.Attributes.OfAttributeType( constructedType )
                .ToArray();

            Assert.Single( attributes );
        }
    }
}
