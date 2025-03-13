// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.CodeModel
{
    public sealed class SyntaxGeneratorTests : UnitTestClass
    {
        private readonly ITestOutputHelper _logger;

        public SyntaxGeneratorTests( ITestOutputHelper logger )
        {
            this._logger = logger;
        }

        [Theory]

        // With nullable context.
        [InlineData( "int?", "typeof(global::System.Int32?)", true )]
        [InlineData( "string?", "typeof(global::System.String)", true )]
        [InlineData( "List<string?>", "typeof(global::System.Collections.Generic.List<global::System.String>)", true )]
        [InlineData( "List<string>?", "typeof(global::System.Collections.Generic.List<global::System.String>)", true )]
        [InlineData(
            "List<List<string?>>",
            "typeof(global::System.Collections.Generic.List<global::System.Collections.Generic.List<global::System.String>>)",
            true )]
        [InlineData( "List<string[]?>", "typeof(global::System.Collections.Generic.List<global::System.String[]>)", true )]
        [InlineData( "List<string?[]?>?", "typeof(global::System.Collections.Generic.List<global::System.String[]>)", true )]
        [InlineData( "List<int[]?>", "typeof(global::System.Collections.Generic.List<global::System.Int32[]>)", true )]
        [InlineData( "List<int?[]>", "typeof(global::System.Collections.Generic.List<global::System.Int32?[]>)", true )]

        // Without nullable context.
        [InlineData( "int?", "typeof(global::System.Int32?)", false )]
        [InlineData( "string?", "typeof(global::System.String)", false )]
        [InlineData( "List<string?>", "typeof(global::System.Collections.Generic.List<global::System.String>)", false )]
        [InlineData( "List<string>?", "typeof(global::System.Collections.Generic.List<global::System.String>)", false )]
        [InlineData(
            "List<List<string?>>",
            "typeof(global::System.Collections.Generic.List<global::System.Collections.Generic.List<global::System.String>>)",
            false )]
        [InlineData( "List<string[]?>", "typeof(global::System.Collections.Generic.List<global::System.String[]>)", false )]
        [InlineData( "List<string?[]?>?", "typeof(global::System.Collections.Generic.List<global::System.String[]>)", false )]
        [InlineData( "List<int[]?>", "typeof(global::System.Collections.Generic.List<global::System.Int32[]>)", false )]
        [InlineData( "List<int?[]>", "typeof(global::System.Collections.Generic.List<global::System.Int32?[]>)", false )]
        [InlineData(
            "global::System.Collections.Generic.List<global::System.String[]?>",
            "typeof(global::System.Collections.Generic.List<global::System.String[]>)",
            false )]
        [InlineData(
            "global::System.Collections.Generic.List<global::System.String?[]?>?",
            "typeof(global::System.Collections.Generic.List<global::System.String[]>)",
            false )]
        [InlineData(
            "global::System.Collections.Generic.List<global::System.Int32[]?>",
            "typeof(global::System.Collections.Generic.List<global::System.Int32[]>)",
            false )]
        [InlineData(
            "global::System.Collections.Generic.List<global::System.Int32?[]>",
            "typeof(global::System.Collections.Generic.List<global::System.Int32?[]>)",
            false )]
        public void TypeOfSyntax( string type, string expectedTypeOf, bool nullable )
        {
            using var testContext = this.CreateTestContext();

            var code = $"using System.Collections.Generic; class T {{ {type} field; }} ";
            var compilation = testContext.CreateCompilationModel( code );
            var fieldType = compilation.Types.Single().Fields.Single().Type;

            var syntaxGenerator = compilation.CompilationContext.GetSyntaxGenerationContext( SyntaxGenerationOptions.Formatted, isNullOblivious: !nullable )
                .SyntaxGenerator;

            var typeOf = syntaxGenerator.TypeOfExpression( fieldType ).ToString();

            this._logger.WriteLine( $"Actual using symbols: {typeOf}" );

            Assert.Equal( expectedTypeOf, typeOf );

            var typeOf2 = syntaxGenerator.TypeOfExpression( fieldType, bypassSymbols: true ).ToString();

            this._logger.WriteLine( $"Actual using IType: {typeOf2}" );

            Assert.Equal( expectedTypeOf, typeOf2 );
        }

        [Theory]

        // With nullable context.
        [InlineData( "int?", "global::System.Int32?", true )]
        [InlineData( "string?", "global::System.String?", true )]
        [InlineData( "List<string?>", "global::System.Collections.Generic.List<global::System.String?>", true )]
        [InlineData( "List<string?>.Enumerator", "global::System.Collections.Generic.List<global::System.String?>.Enumerator", true )]
        [InlineData( "List<string>?", "global::System.Collections.Generic.List<global::System.String>?", true )]
        [InlineData(
            "List<List<string?>>",
            "global::System.Collections.Generic.List<global::System.Collections.Generic.List<global::System.String?>>",
            true )]
        [InlineData( "List<string[]?>", "global::System.Collections.Generic.List<global::System.String[]?>", true )]
        [InlineData( "List<string?[]?>?", "global::System.Collections.Generic.List<global::System.String?[]?>?", true )]
        [InlineData( "List<int[]?>", "global::System.Collections.Generic.List<global::System.Int32[]?>", true )]
        [InlineData( "List<int?[]>", "global::System.Collections.Generic.List<global::System.Int32?[]>", true )]

        // Without nullable context.
        [InlineData( "int?", "global::System.Int32?", false )]
        [InlineData( "string?", "global::System.String", false )]
        [InlineData( "List<string?>", "global::System.Collections.Generic.List<global::System.String>", false )]
        [InlineData( "List<string?>.Enumerator", "global::System.Collections.Generic.List<global::System.String>.Enumerator", false )]
        [InlineData( "List<string>?", "global::System.Collections.Generic.List<global::System.String>", false )]
        [InlineData(
            "List<List<string?>>",
            "global::System.Collections.Generic.List<global::System.Collections.Generic.List<global::System.String>>",
            false )]
        [InlineData( "List<string[]?>", "global::System.Collections.Generic.List<global::System.String[]>", false )]
        [InlineData( "List<string?[]?>?", "global::System.Collections.Generic.List<global::System.String[]>", false )]
        [InlineData( "List<int[]?>", "global::System.Collections.Generic.List<global::System.Int32[]>", false )]
        [InlineData( "List<int?[]>", "global::System.Collections.Generic.List<global::System.Int32?[]>", false )]
        public void TypeSyntax( string type, string expectedTypeOf, bool nullable )
        {
            using var testContext = this.CreateTestContext();

            var code = $"using System.Collections.Generic; class T {{ {type} field; }} ";
            var compilation = testContext.CreateCompilationModel( code );
            var fieldType = compilation.Types.Single().Fields.Single().Type;

            var syntaxGenerator = compilation.CompilationContext.GetSyntaxGenerationContext( SyntaxGenerationOptions.Formatted, isNullOblivious: !nullable )
                .SyntaxGenerator;

            var typeSyntax = syntaxGenerator.TypeSyntax( fieldType ).ToString();

            this._logger.WriteLine( $"Actual using symbols: {typeSyntax}" );

            Assert.Equal( expectedTypeOf, typeSyntax );

            var typeSyntax2 = syntaxGenerator.TypeSyntax( fieldType, bypassSymbols: true ).ToString();

            this._logger.WriteLine( $"Actual using IType: {typeSyntax2}" );

            Assert.Equal( expectedTypeOf, typeSyntax2 );
        }

        [Theory]

        // With nullable context.
        [InlineData( "int", "global::System.Int32", true )]
        [InlineData( "string", "global::System.String", true )]
        [InlineData( "List<string?>", "global::System.Collections.Generic.List<global::System.String?>", true )]
        [InlineData( "List<string?>.Enumerator", "global::System.Collections.Generic.List<global::System.String?>.Enumerator", true )]
        [InlineData( "List<string>?", "global::System.Collections.Generic.List<global::System.String>", true )]
        [InlineData(
            "List<List<string?>>",
            "global::System.Collections.Generic.List<global::System.Collections.Generic.List<global::System.String?>>",
            true )]
        [InlineData( "List<string[]?>", "global::System.Collections.Generic.List<global::System.String[]?>", true )]
        [InlineData( "List<string?[]?>?", "global::System.Collections.Generic.List<global::System.String?[]?>", true )]
        [InlineData( "List<int[]?>", "global::System.Collections.Generic.List<global::System.Int32[]?>", true )]
        [InlineData( "List<int?[]>", "global::System.Collections.Generic.List<global::System.Int32?[]>", true )]

        // Without nullable context.
        [InlineData( "int?", "global::System.Int32?", false )]
        [InlineData( "string?", "global::System.String", false )]
        [InlineData( "List<string?>", "global::System.Collections.Generic.List<global::System.String>", false )]
        [InlineData( "List<string?>.Enumerator", "global::System.Collections.Generic.List<global::System.String>.Enumerator", false )]
        [InlineData( "List<string>?", "global::System.Collections.Generic.List<global::System.String>", false )]
        [InlineData(
            "List<List<string?>>",
            "global::System.Collections.Generic.List<global::System.Collections.Generic.List<global::System.String>>",
            false )]
        [InlineData( "List<string[]?>", "global::System.Collections.Generic.List<global::System.String[]>", false )]
        [InlineData( "List<string?[]?>?", "global::System.Collections.Generic.List<global::System.String[]>", false )]
        [InlineData( "List<int[]?>", "global::System.Collections.Generic.List<global::System.Int32[]>", false )]
        [InlineData( "List<int?[]>", "global::System.Collections.Generic.List<global::System.Int32?[]>", false )]
        public void TypeExpression( string type, string expectedTypeOf, bool nullableContext )
        {
            using var testContext = this.CreateTestContext();

            var code = $"using System.Collections.Generic; class T {{ {type} field; }} ";
            var compilation = testContext.CreateCompilationModel( code );
            var fieldType = compilation.Types.Single().Fields.Single().Type;

            var syntaxGenerator = compilation.CompilationContext
                .GetSyntaxGenerationContext( SyntaxGenerationOptions.Formatted, isNullOblivious: !nullableContext )
                .SyntaxGenerator;

            var typeSyntax = syntaxGenerator.TypeExpression( fieldType );

            this._logger.WriteLine( $"Actual using symbols: {typeSyntax}" );

            Assert.Equal( expectedTypeOf, typeSyntax.ToString() );

            var typeSyntax2 = syntaxGenerator.TypeExpression( fieldType, bypassSymbols: true );

            this._logger.WriteLine( $"Actual using IType: {typeSyntax2}" );

            Assert.Equal( expectedTypeOf, typeSyntax2.ToString() );
        }

        [Fact]
        public void UnboundGenericType()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCompilationModel( "" );

            var type = compilation.Factory.GetTypeByReflectionType( typeof(Dictionary<,>) );

            var syntaxGenerator = compilation.CompilationContext.GetSyntaxGenerationContext( SyntaxGenerationOptions.Formatted ).SyntaxGenerator;

            var typeSyntax = syntaxGenerator.TypeSyntax( type ).ToString();

            this._logger.WriteLine( $"Actual using symbols: {typeSyntax}" );

            const string expected = "global::System.Collections.Generic.Dictionary<TKey,TValue>";
            Assert.Equal( expected, typeSyntax );

            var typeSyntax2 = syntaxGenerator.TypeSyntax( type, bypassSymbols: true ).ToString();

            this._logger.WriteLine( $"Actual using IType: {typeSyntax2}" );

            Assert.Equal( expected, typeSyntax2 );
        }

        [Theory]
        [InlineData( "0", "0" )]
        [InlineData( "null", "default(global::System.Object)" )]
        [InlineData( "typeof(string)", "typeof(global::System.String)" )]
        [InlineData( "DayOfWeek.Monday", "global::System.DayOfWeek.Monday" )]
        [InlineData( "new[] { 0 }", "new global::System.Int32[] { 0 }" )]
        [InlineData( "new[] { (string?) null }", "new global::System.String[] { null }" )]
        [InlineData( "new[] { typeof(string) }", "new global::System.Type[] { typeof(global::System.String) }" )]
        [InlineData( "new[] { DayOfWeek.Monday }", "new global::System.DayOfWeek[] { global::System.DayOfWeek.Monday }" )]
        public void AttributeValue( string inputSyntax, string expectedOutputSyntax )
        {
            using var testContext = this.CreateTestContext();

            var code =
                $"using System; class MyAttribute : Attribute {{ public MyAttribute( object value ) {{}} }} [MyAttribute( {inputSyntax} )] class C {{}} ";

            var compilation = testContext.CreateCompilationModel( code );
            var syntaxGenerationContext = compilation.CompilationContext.GetSyntaxGenerationContext( SyntaxGenerationOptions.Formatted );
            var syntaxGenerator = syntaxGenerationContext.SyntaxGenerator;
            var type = compilation.Types.OfName( "C" ).Single();
            var attribute = type.Attributes.Single();
            var codeModelOutput = syntaxGenerator.Attribute( attribute ).ArgumentList!.Arguments[0].NormalizeWhitespace().ToFullString();
            Assert.Equal( expectedOutputSyntax, codeModelOutput );
        }

        [Fact]
        public void ParamsAttributeValue()
        {
            using var testContext = this.CreateTestContext();

            const string code = """
                                using System;

                                class MyAttribute( params int[] Values ) : Attribute;

                                [MyAttribute( null )]
                                class C;
                                """;

            var compilation = testContext.CreateCompilationModel( code );
            var syntaxGenerationContext = compilation.CompilationContext.GetSyntaxGenerationContext( SyntaxGenerationOptions.Formatted );
            var syntaxGenerator = syntaxGenerationContext.SyntaxGenerator;
            var type = compilation.Types.OfName( "C" ).Single();
            var attribute = type.Attributes.Single();
            var codeModelOutput = syntaxGenerator.Attribute( attribute ).ArgumentList!.Arguments[0].NormalizeWhitespace().ToFullString();
            Assert.Equal( "default(global::System.Int32[])", codeModelOutput );
        }

        [Theory]
        [InlineData( "", "" )]
        [InlineData( "where T : notnull", "where T : notnull" )]
        [InlineData( "where T : class", "where T : class" )]
        [InlineData( "where T : struct", "where T : struct" )]
        [InlineData( "where T : unmanaged", "where T : unmanaged" )]
        [InlineData( "where T : class?", "where T : class?" )]
        [InlineData( "where T : IDisposable", "where T : global::System.IDisposable" )]
        [InlineData( "where T : IDisposable?", "where T : global::System.IDisposable?" )]
        [InlineData( "where T : IDisposable, new()", "where T : global::System.IDisposable, new()" )]
        public void GenericConstraints( string input, string expected )
        {
            using var testContext = this.CreateTestContext();

            var code =
                $"using System; class C {{ void M<T>() {input} {{}} }} ";

            var compilation = testContext.CreateCompilationModel( code );
            var method = compilation.Types.Single().Methods.Single().GetSymbol().AssertNotNull();
            var syntaxGenerationContext = compilation.CompilationContext.GetSyntaxGenerationContext( SyntaxGenerationOptions.Formatted );
            var syntaxGenerator = syntaxGenerationContext.SyntaxGenerator;

            var syntax = syntaxGenerator.TypeParameterConstraintClauses( method.TypeParameters );
            Assert.Equal( expected, syntax.ToString() );
        }
    }
}