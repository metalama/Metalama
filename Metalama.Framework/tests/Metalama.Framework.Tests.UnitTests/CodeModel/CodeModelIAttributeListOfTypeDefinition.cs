// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel
{
    public sealed class CodeModelIAttributeListOfTypeDefinition : UnitTestClass
    {
        [Fact]
        public void NonGeneric()
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

            var types = type.Attributes.OfAttributeType( attributeType, ConversionKind.TypeDefinition )
                .Select( x => x.Type.Definition )
                .OrderBy( x => x.GetSymbol(), StructuralSymbolComparer.Default )
                .ToArray();

            Assert.Equal( new[] { attributeType }, types );
        }

        [Fact]
        public void Generic()
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

            var types = type.Attributes.OfAttributeType( attributeType, ConversionKind.TypeDefinition )
                .Select( x => x.Type.Definition )
                .OrderBy( x => x.GetSymbol(), StructuralSymbolComparer.Default )
                .ToArray();

            Assert.Equal( new[] { attributeType }, types );
        }

        [Fact]
        public void GenericBase()
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
            var usedType = compilation.Types.Single( t => t.Name == "B" );
            var type = compilation.Types.Single( t => t.Name == "C" );

            var types = type.Attributes.OfAttributeType( attributeType, ConversionKind.TypeDefinition )
                .Select( x => x.Type.Definition )
                .OrderBy( x => x.GetSymbol(), StructuralSymbolComparer.Default )
                .ToArray();

            Assert.Equal( new[] { usedType }, types );
        }

        [Fact]
        public void GenericInterface()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
interface A<T>
{
}

class B : System.Attribute, A<int>
{
}

[B]
class C
{
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var interfaceType = compilation.Types.Single( t => t.Name == "A" );
            var usedType = compilation.Types.Single( t => t.Name == "B" );
            var type = compilation.Types.Single( t => t.Name == "C" );

            var types = type.Attributes.OfAttributeType( interfaceType, ConversionKind.TypeDefinition )
                .Select( x => x.Type.Definition )
                .OrderBy( x => x.GetSymbol(), StructuralSymbolComparer.Default )
                .ToArray();

            Assert.Equal( new[] { usedType }, types );
        }
    }
}