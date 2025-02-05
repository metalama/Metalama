// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Source.ConstructedTypes;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;
using RefKind = Microsoft.CodeAnalysis.RefKind;
using SpecialType = Metalama.Framework.Code.SpecialType;

namespace Metalama.Framework.Tests.UnitTests.CodeModel
{
    public sealed class ToTypeOfExpressionTests : UnitTestClass
    {
        [Fact]
        public void TestSpecial()
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( "/* nothing */" );
            var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

            Assert.Equal( "typeof(void)", GetSyntaxString( syntaxSerializationContext, compilation.Factory.GetSpecialType( SpecialType.Void ) ) );

            Assert.Equal(
                "typeof(global::System.Int32)",
                GetSyntaxString( syntaxSerializationContext, compilation.Factory.GetSpecialType( SpecialType.Int32 ) ) );

            Assert.Equal(
                "typeof(global::System.Object)",
                GetSyntaxString( syntaxSerializationContext, compilation.Factory.GetSpecialType( SpecialType.Object ) ) );

            Assert.Equal(
                "typeof(global::System.String)",
                GetSyntaxString( syntaxSerializationContext, compilation.Factory.GetSpecialType( SpecialType.String ) ) );
        }

        [Fact]
        public void TestPointer()
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( "/* nothing */" );
            var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

            Assert.Equal(
                "typeof(global::System.Int32*)",
                GetSyntaxString( syntaxSerializationContext, compilation.Factory.GetSpecialType( SpecialType.Int32 ).MakePointerType() ) );
        }

        [Fact]
        public void TestArray()
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( "/* nothing */" );
            var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

            Assert.Equal(
                "typeof(global::System.Int32[])",
                GetSyntaxString( syntaxSerializationContext, compilation.Factory.GetSpecialType( SpecialType.Int32 ).MakeArrayType() ) );

            Assert.Equal(
                "typeof(global::System.Int32[,])",
                GetSyntaxString( syntaxSerializationContext, compilation.Factory.GetSpecialType( SpecialType.Int32 ).MakeArrayType( 2 ) ) );
        }

        [Fact]
        public void TestFunctionPointer()
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( "/* nothing */" );
            var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

            var functionPointerTypeSymbol = compilation.GetRoslynCompilation()
                .CreateFunctionPointerTypeSymbol(
                    compilation.Factory.GetSpecialType( SpecialType.Int32 ).GetSymbol(),
                    RefKind.None,
                    [(ITypeSymbol) compilation.Factory.GetSpecialType( SpecialType.Int32 ).GetSymbol()],
                    [RefKind.None] );

            var functionPointerType = new SymbolFunctionPointerType( functionPointerTypeSymbol, compilation, GenericContext.Empty );

            Assert.Equal( "typeof(delegate*<global::System.Int32,global::System.Int32>)", GetSyntaxString( syntaxSerializationContext, functionPointerType ) );
        }

        [Fact]
        public void TestUser()
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( "namespace N { class A { } }" );
            var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

            Assert.Equal( "typeof(global::N.A)", GetSyntaxString( syntaxSerializationContext, compilation.Types.OfName( "A" ).Single() ) );
        }

        [Fact]
        public void TestOpenGeneric()
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( "namespace N { class A<T,U,V> { } }" );
            var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

            Assert.Equal(
                "typeof(global::System.Collections.Generic.List<>)",
                GetSyntaxString( syntaxSerializationContext, compilation.Factory.GetSpecialType( SpecialType.List_T ) ) );

            Assert.Equal( "typeof(global::N.A<,,>)", GetSyntaxString( syntaxSerializationContext, compilation.Types.OfName( "A" ).Single() ) );
        }

        [Fact]
        public void TestClosedGeneric()
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( "namespace N { class A<T,U,V> { } }" );
            var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );
            var argumentType = compilation.Factory.GetSpecialType( SpecialType.Int32 );

            Assert.Equal(
                "typeof(global::System.Collections.Generic.List<global::System.Int32>)",
                GetSyntaxString( syntaxSerializationContext, compilation.Factory.GetSpecialType( SpecialType.List_T ).WithTypeArguments( argumentType ) ) );

            Assert.Equal(
                "typeof(global::N.A<global::System.Int32,global::System.Int32,global::System.Int32>)",
                GetSyntaxString(
                    syntaxSerializationContext,
                    compilation.Types.OfName( "A" ).Single().WithTypeArguments( argumentType, argumentType, argumentType ) ) );
        }

        [Fact]
        public void TestGenericArguments()
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( "namespace N { class A<T,U> { void Foo(A<U,T> x) {}} }" );
            var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

            var parameterType = compilation.Types.Single().Methods.Single().Parameters.Single().Type;

            Assert.Equal( "typeof(global::N.A<U,T>)", GetSyntaxString( syntaxSerializationContext, parameterType ) );
        }

        private static string GetSyntaxString( SyntaxSerializationContext syntaxSerializationContext, IType type )
            => ((IUserExpression) type.ToTypeOfExpression()).ToTypedExpressionSyntax( syntaxSerializationContext ).Syntax.ToFullString();
    }
}