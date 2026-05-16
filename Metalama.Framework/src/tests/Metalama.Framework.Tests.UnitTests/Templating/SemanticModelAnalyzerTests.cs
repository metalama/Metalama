// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Templating;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating
{
    public sealed class SemanticModelAnalyzerTests : UnitTestClass
    {
        [Fact]
        public void RuntimeCodeCallingCompileTimeOnlyMethod()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCSharpCompilation(
                """
                using Metalama.Framework.Code;

                class X { void M() { IMethod m; } }
                """ );

            List<Diagnostic> diagnostics = new();
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel( syntaxTree );

            TemplatingCodeValidator.Validate(
                testContext.ServiceProvider,
                semanticModel,
                diagnostics.Add,
                null,
                false,
                false,
                testContext.CancellationToken );

            Assert.Contains( diagnostics, d => d.Id == TemplatingDiagnosticDescriptors.CannotReferenceCompileTimeOnly.Id );
        }

        [Fact]
        public void RuntimeCodeCallingCompileTimeOnlyMethod_SkippedWhenValidationDisabled()
        {
            using var testContext = this.CreateTestContext( new TestContextOptions { ValidateRunTimeCode = false } );

            var compilation = testContext.CreateCSharpCompilation(
                """
                using Metalama.Framework.Code;

                class X { void M() { IMethod m; } }
                """ );

            List<Diagnostic> diagnostics = new();
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel( syntaxTree );

            TemplatingCodeValidator.Validate(
                testContext.ServiceProvider,
                semanticModel,
                diagnostics.Add,
                null,
                false,
                false,
                testContext.CancellationToken );

            // No diagnostics should be reported since validation is disabled for run-time code.
            Assert.DoesNotContain( diagnostics, d => d.Id == TemplatingDiagnosticDescriptors.CannotReferenceCompileTimeOnly.Id );
        }

        [Fact]
        public void RuntimePropertyBody_SkippedWhenValidationDisabled()
        {
            using var testContext = this.CreateTestContext( new TestContextOptions { ValidateRunTimeCode = false } );

            var compilation = testContext.CreateCSharpCompilation(
                """
                using Metalama.Framework.Code;

                class X
                {
                    // Property body references compile-time type (signature type is run-time).
                    object P { get { IMethod m = null; return m; } }

                    // Expression-bodied property references compile-time type.
                    object P2 => typeof(IMethod);
                }
                """ );

            List<Diagnostic> diagnostics = new();
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel( syntaxTree );

            TemplatingCodeValidator.Validate(
                testContext.ServiceProvider,
                semanticModel,
                diagnostics.Add,
                null,
                false,
                false,
                testContext.CancellationToken );

            // No diagnostics should be reported since validation is disabled for run-time code.
            Assert.DoesNotContain( diagnostics, d => d.Id == TemplatingDiagnosticDescriptors.CannotReferenceCompileTimeOnly.Id );
        }

        [Fact]
        public void RuntimeField_SkippedWhenValidationDisabled()
        {
            using var testContext = this.CreateTestContext( new TestContextOptions { ValidateRunTimeCode = false } );

            var compilation = testContext.CreateCSharpCompilation(
                """
                using Metalama.Framework.Code;

                class X
                {
                    object _field = typeof(IMethod);  // Field initializer with compile-time type reference
                }
                """ );

            List<Diagnostic> diagnostics = new();
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel( syntaxTree );

            TemplatingCodeValidator.Validate(
                testContext.ServiceProvider,
                semanticModel,
                diagnostics.Add,
                null,
                false,
                false,
                testContext.CancellationToken );

            // No diagnostics should be reported since validation is disabled for run-time code.
            Assert.DoesNotContain( diagnostics, d => d.Id == TemplatingDiagnosticDescriptors.CannotReferenceCompileTimeOnly.Id );
        }

        [Fact]
        public void RuntimeConstructor_SkippedWhenValidationDisabled()
        {
            using var testContext = this.CreateTestContext( new TestContextOptions { ValidateRunTimeCode = false } );

            var compilation = testContext.CreateCSharpCompilation(
                """
                using Metalama.Framework.Code;

                class X
                {
                    public X() { IMethod m; }  // Constructor body with compile-time type
                }
                """ );

            List<Diagnostic> diagnostics = new();
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel( syntaxTree );

            TemplatingCodeValidator.Validate(
                testContext.ServiceProvider,
                semanticModel,
                diagnostics.Add,
                null,
                false,
                false,
                testContext.CancellationToken );

            // No diagnostics should be reported since validation is disabled for run-time code.
            Assert.DoesNotContain( diagnostics, d => d.Id == TemplatingDiagnosticDescriptors.CannotReferenceCompileTimeOnly.Id );
        }

        [Fact]
        public void RuntimeLocalFunction_SkippedWhenValidationDisabled()
        {
            using var testContext = this.CreateTestContext( new TestContextOptions { ValidateRunTimeCode = false } );

            var compilation = testContext.CreateCSharpCompilation(
                """
                using Metalama.Framework.Code;

                class X
                {
                    void M()
                    {
                        void Local() { IMethod m; }  // Local function body with compile-time type
                    }
                }
                """ );

            List<Diagnostic> diagnostics = new();
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel( syntaxTree );

            TemplatingCodeValidator.Validate(
                testContext.ServiceProvider,
                semanticModel,
                diagnostics.Add,
                null,
                false,
                false,
                testContext.CancellationToken );

            // No diagnostics should be reported since validation is disabled for run-time code.
            Assert.DoesNotContain( diagnostics, d => d.Id == TemplatingDiagnosticDescriptors.CannotReferenceCompileTimeOnly.Id );
        }

        [Fact]
        public void RuntimePropertyWithAccessorBodies_SkippedWhenValidationDisabled()
        {
            using var testContext = this.CreateTestContext( new TestContextOptions { ValidateRunTimeCode = false } );

            var compilation = testContext.CreateCSharpCompilation(
                """
                using Metalama.Framework.Code;

                class X
                {
                    private object _value;

                    // Property with explicit accessor bodies referencing compile-time type.
                    object P
                    {
                        get { IMethod m = null; return m; }
                        set { IMethod m = null; _value = m; }
                    }

                    // Property with expression-bodied accessors.
                    object P2
                    {
                        get => typeof(IMethod);
                        set => _value = typeof(IMethod);
                    }
                }
                """ );

            List<Diagnostic> diagnostics = new();
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel( syntaxTree );

            TemplatingCodeValidator.Validate(
                testContext.ServiceProvider,
                semanticModel,
                diagnostics.Add,
                null,
                false,
                false,
                testContext.CancellationToken );

            // No diagnostics should be reported since validation is disabled for run-time code.
            Assert.DoesNotContain( diagnostics, d => d.Id == TemplatingDiagnosticDescriptors.CannotReferenceCompileTimeOnly.Id );
        }

        [Fact]
        public void RuntimeIndexerWithAccessorBodies_SkippedWhenValidationDisabled()
        {
            using var testContext = this.CreateTestContext( new TestContextOptions { ValidateRunTimeCode = false } );

            var compilation = testContext.CreateCSharpCompilation(
                """
                using Metalama.Framework.Code;

                class X
                {
                    private object _value;

                    // Indexer with accessor bodies referencing compile-time type.
                    object this[int i]
                    {
                        get { IMethod m = null; return m; }
                        set { IMethod m = null; _value = m; }
                    }
                }
                """ );

            List<Diagnostic> diagnostics = new();
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel( syntaxTree );

            TemplatingCodeValidator.Validate(
                testContext.ServiceProvider,
                semanticModel,
                diagnostics.Add,
                null,
                false,
                false,
                testContext.CancellationToken );

            // No diagnostics should be reported since validation is disabled for run-time code.
            Assert.DoesNotContain( diagnostics, d => d.Id == TemplatingDiagnosticDescriptors.CannotReferenceCompileTimeOnly.Id );
        }

        [Fact]
        public void RuntimeExpressionBodiedIndexer_SkippedWhenValidationDisabled()
        {
            using var testContext = this.CreateTestContext( new TestContextOptions { ValidateRunTimeCode = false } );

            var compilation = testContext.CreateCSharpCompilation(
                """
                using Metalama.Framework.Code;

                class X
                {
                    // Expression-bodied get-only indexer.
                    object this[int i] => typeof(IMethod);
                }
                """ );

            List<Diagnostic> diagnostics = new();
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel( syntaxTree );

            TemplatingCodeValidator.Validate(
                testContext.ServiceProvider,
                semanticModel,
                diagnostics.Add,
                null,
                false,
                false,
                testContext.CancellationToken );

            // No diagnostics should be reported since validation is disabled for run-time code.
            Assert.DoesNotContain( diagnostics, d => d.Id == TemplatingDiagnosticDescriptors.CannotReferenceCompileTimeOnly.Id );
        }

        [Fact]
        public void MustImportNamespace()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCSharpCompilation(
                """
                class X : Metalama.Framework.Aspects.OverrideMethodAspect {  public override dynamic? OverrideMethod() { return null; } }
                """ );

            List<Diagnostic> diagnostics = new();
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel( syntaxTree );

            TemplatingCodeValidator.Validate(
                testContext.ServiceProvider,
                semanticModel,
                diagnostics.Add,
                null,
                false,
                false,
                testContext.CancellationToken );

            Assert.Contains( diagnostics, d => d.Id == TemplatingDiagnosticDescriptors.CompileTimeCodeNeedsNamespaceImport.Id );
        }

        [Fact]
        public void InvalidBaseTypeArraySyntax_DoesNotThrow()
        {
            // Regression test for https://github.com/metalama/Metalama/issues/649
            // When a class uses invalid "array" syntax in base type (e.g. OverrideMethodAspect[]),
            // Roslyn parses the base type as ArrayTypeSymbol. The validator should not crash.
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCSharpCompilation(
                """
                using Metalama.Framework.Aspects;

                public class SECall : OverrideMethodAspect[]
                {
                }

                public class SECall2 : OverrideMethodAspect[c]
                {
                }
                """,
                ignoreErrors: true );

            List<Diagnostic> diagnostics = new();
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel( syntaxTree );

            // This should not throw an InvalidCastException.
            TemplatingCodeValidator.Validate(
                testContext.ServiceProvider,
                semanticModel,
                diagnostics.Add,
                null,
                false,
                false,
                testContext.CancellationToken );
        }

        [Fact]
        public void RoslynTypeInRunTimeCode_ReportsLama0291_WhenRoslynIsCompileTimeOnly()
        {
            using var testContext = this.CreateTestContext( new TestContextOptions { RoslynIsCompileTimeOnly = true } );

            var roslynReference = MetadataReference.CreateFromFile( typeof(ISymbol).Assembly.Location );

            var compilation = testContext.CreateCSharpCompilation(
                """
                using Microsoft.CodeAnalysis;

                class X { void M() { ISymbol s; } }
                """,
                additionalReferences: new[] { roslynReference } );

            List<Diagnostic> diagnostics = new();
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel( syntaxTree );

            TemplatingCodeValidator.Validate(
                testContext.ServiceProvider,
                semanticModel,
                diagnostics.Add,
                null,
                false,
                false,
                testContext.CancellationToken );

            Assert.Contains( diagnostics, d => d.Id == TemplatingDiagnosticDescriptors.CannotReferenceCompileTimeOnlyRoslyn.Id );
            Assert.DoesNotContain( diagnostics, d => d.Id == TemplatingDiagnosticDescriptors.CannotReferenceCompileTimeOnly.Id );
        }

        [Fact]
        public void RoslynTypeInRunTimeCode_NoDiagnostic_WhenRoslynIsNotCompileTimeOnly()
        {
            // Default behavior (RoslynIsCompileTimeOnly=false): Roslyn types are allowed in run-time code.
            using var testContext = this.CreateTestContext();

            var roslynReference = MetadataReference.CreateFromFile( typeof(ISymbol).Assembly.Location );

            var compilation = testContext.CreateCSharpCompilation(
                """
                using Microsoft.CodeAnalysis;

                class X { void M() { ISymbol s; } }
                """,
                additionalReferences: new[] { roslynReference } );

            List<Diagnostic> diagnostics = new();
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel( syntaxTree );

            TemplatingCodeValidator.Validate(
                testContext.ServiceProvider,
                semanticModel,
                diagnostics.Add,
                null,
                false,
                false,
                testContext.CancellationToken );

            Assert.DoesNotContain( diagnostics, d => d.Id == TemplatingDiagnosticDescriptors.CannotReferenceCompileTimeOnlyRoslyn.Id );
            Assert.DoesNotContain( diagnostics, d => d.Id == TemplatingDiagnosticDescriptors.CannotReferenceCompileTimeOnly.Id );
        }

        [Fact]
        public void NoMetalamaReference_DoesNotThrow()
        {
            // Regression test for https://github.com/metalama/Metalama/issues/1630.
            // While the IDE is open, a 'git clean' can momentarily leave a compilation that still has the
            // Metalama analyzer loaded but no metadata reference to Metalama.Framework. The validator must
            // degrade to a no-op instead of crashing.
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCSharpCompilation(
                """
                class X { void M() {} }
                """,
                addMetalamaReferences: false );

            List<Diagnostic> diagnostics = new();
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel( syntaxTree );

            // This should not throw.
            TemplatingCodeValidator.Validate(
                testContext.ServiceProvider,
                semanticModel,
                diagnostics.Add,
                null,
                false,
                false,
                testContext.CancellationToken );

            Assert.Empty( diagnostics );
        }

        [Fact]
        public void WellKnownTypesNotReported()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCSharpCompilation(
                """
                using System;

                namespace Microsoft.CodeAnalysis
                {
                    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate )]
                    internal sealed partial class EmbeddedAttribute : Attribute
                    {
                    }
                }
                """ );

            List<Diagnostic> diagnostics = new();
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel( syntaxTree );

            TemplatingCodeValidator.Validate(
                testContext.ServiceProvider,
                semanticModel,
                diagnostics.Add,
                null,
                false,
                false,
                testContext.CancellationToken );

            Assert.Empty( diagnostics );
        }
    }
}