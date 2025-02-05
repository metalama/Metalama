// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization
{
    public abstract class SerializerTestsBase : UnitTestClass
    {
        /// <summary>
        /// A value indicating whether tests that test the serialization of reflection objects like <see cref="Type"/> should use "dotnet build" to see if the
        /// resulting syntax tree actually compiles and results in valid IL. This is slow but necessary during development, at least, since an incorrect syntax tree
        /// can easily be produced.
        /// </summary>
        private const bool _doCodeExecutionTests = false;

        protected object? ExecuteExpression( string context, string expression )
        {
            using var testContext = this.CreateTestContext();

            var expressionContainer = $@"
class Expression
{{
    public static object Execute() => {expression};
}}";

            var assemblyPath = MetalamaCompilerUtility.CompileAssembly(
                testContext.ServiceProvider,
                testContext.BaseDirectory,
                context,
                expressionContainer );

            var assembly = testContext.Domain.LoadAssembly( assemblyPath );

            return assembly.GetType( "Expression" )!.GetMethod( "Execute" )!.Invoke( null, null );
        }

        /// <summary>
        /// Executes the C# <paramref name="expression"/> alongside the code <paramref name="context"/> and passes the value of the expression
        /// as the argument to the callback <paramref name="withResult"/>. Does all of this only conditionally: it does nothing if <see cref="_doCodeExecutionTests"/>
        /// is false.
        /// </summary>
        /// <param name="context">Additional C# code.</param>
        /// <param name="expression">A C# expression of type <typeparamref name="T"/>.</param>
        /// <param name="withResult">Code to run on the result of the expression.</param>
        protected void TestExpression<T>( string context, string expression, Action<T> withResult )
        {
#pragma warning disable CS0162 // Unreachable code detected

            // ReSharper disable HeuristicUnreachableCode

            if ( _doCodeExecutionTests )
            {
                var t = (T) this.ExecuteExpression( context, expression )!;
                withResult( t );
            }
#pragma warning restore CS0162 // Unreachable code detected
        }

        private TestContextOptions CreateProjectOptions() => new() { AdditionalAssemblies = ImmutableArray.Create( this.GetType().Assembly ) };

        private protected SerializerTestContext CreateSerializationTestContext( string code ) => new( code, this.CreateProjectOptions() );

        private protected SerializerTestContext CreateSerializationTestContext( CompilationModel compilation )
            => new( compilation, this.CreateProjectOptions() );

        protected SerializerTestsBase( ITestOutputHelper? logger = null ) : base( logger ) { }

        private protected sealed class SerializerTestContext : TestContext
        {
            public CompilationModel Compilation { get; }

            public SerializerTestContext( CompilationModel compilationModel, TestContextOptions contextOptions ) : base( contextOptions )
            {
                this.Compilation = compilationModel;

                // We need a syntax factory for an arbitrary compilation, but at least with standard references.
                // Note that we cannot easily get a reference to Metalama.Compiler.Interfaces this way because we have a reference assembly.

                this.SerializationContext = new SyntaxSerializationContext( this.Compilation, SyntaxGenerationOptions.Formatted );

                this.SerializationService = new SyntaxSerializationService();
            }

            public SerializerTestContext( string code, TestContextOptions contextOptions ) : base( contextOptions )
            {
                this.Compilation = this.CreateCompilationModel( code );

                // We need a syntax factory for an arbitrary compilation, but at least with standard references.
                // Note that we cannot easily get a reference to Metalama.Compiler.Interfaces this way because we have a reference assembly.

                this.SerializationContext = new SyntaxSerializationContext( this.Compilation, SyntaxGenerationOptions.Formatted );

                this.SerializationService = new SyntaxSerializationService();
            }

            public SyntaxSerializationContext SerializationContext { get; }

            public SyntaxSerializationService SerializationService { get; }

            public ExpressionSyntax Serialize<T>( T o ) => this.SerializationService.Serialize( o, this.SerializationContext );
        }
    }
}