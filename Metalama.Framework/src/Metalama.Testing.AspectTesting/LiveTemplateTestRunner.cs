// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Pipeline.LiveTemplates;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Testing.AspectTesting
{
    internal sealed class LiveTemplateTestRunner : BaseTestRunner
    {
        public LiveTemplateTestRunner(
            GlobalServiceProvider serviceProvider,
            string? projectDirectory,
            TestProjectReferences references,
            ITestOutputHelper? logger )
            : base( serviceProvider, projectDirectory, references, logger ) { }

        protected override async Task RunAsync(
            TestInput testInput,
            TestResult testResult,
            TestContext testContext )
        {
            Assert.True( testInput.Options.TestScenario is TestScenario.LiveTemplate or TestScenario.LiveTemplatePreview );

            await base.RunAsync( testInput, testResult, testContext );

            var serviceProvider = testContext.ServiceProvider;

            var partialCompilation = PartialCompilation.CreateComplete( testResult.InputCompilation! );

            var targets = new List<(ISymbol Target, INamedTypeSymbol AspectType)>();

            foreach ( var syntaxTree in partialCompilation.SyntaxTrees.Values )
            {
                var semanticModel = partialCompilation.Compilation.GetCachedSemanticModel( syntaxTree );
                new TargetAttributeWalker( semanticModel, targets.Add ).Visit( await syntaxTree.GetRootAsync() );
            }

            switch ( targets.Count )
            {
                case 0:
                    throw new InvalidTestTargetException( $"No [{nameof(TestLiveTemplateAttribute)}] was found." );

                case > 1:
                    throw new InvalidTestTargetException( $"More than one [{nameof(TestLiveTemplateAttribute)}] were found." );
            }

            var target = targets[0];

            var result = await LiveTemplateAspectPipeline.ExecuteAsync(
                serviceProvider,
                null,
                c => c.AspectClasses.Single( a => a.ShortName == target.AspectType.Name ),
                partialCompilation,
                target.Target,
                testResult.PipelineDiagnostics );

            if ( result.IsSuccessful )
            {
                testResult.HasOutputCode = true;

                var formattedOutputCompilation = await new CodeFormatter().FormatAsync( result.Value, CancellationToken.None );

                var targetSyntaxTree = target.Target.GetPrimarySyntaxReference().AssertNotNull().SyntaxTree;

                var inputSyntaxTreeIndex = -1;
                TestSyntaxTree testSyntaxTree;

                do
                {
                    testSyntaxTree = testResult.SyntaxTrees.ElementAt( ++inputSyntaxTreeIndex );
                }
                while ( testSyntaxTree.InputSyntaxTree != targetSyntaxTree );

                var transformedSyntaxTree = formattedOutputCompilation.Compilation.SyntaxTrees.ElementAt( inputSyntaxTreeIndex );

                var transformedSyntaxRoot = await transformedSyntaxTree.GetRootAsync();

                await testSyntaxTree.SetRunTimeCodeAsync( transformedSyntaxRoot );
            }
            else
            {
                testResult.SetFailed( "LiveTemplateAspectPipeline.TryExecute failed." );
            }
        }

        private sealed class TargetAttributeWalker : CSharpSyntaxWalker
        {
            private readonly SemanticModel _semanticModel;
            private readonly Action<(ISymbol Target, INamedTypeSymbol AspectType)> _addTarget;

            public TargetAttributeWalker( SemanticModel semanticModel, Action<(ISymbol Target, INamedTypeSymbol AspectType)> addTarget )
            {
                this._semanticModel = semanticModel;
                this._addTarget = addTarget;
            }

            public override void VisitAttribute( AttributeSyntax node )
            {
                var attributeSymbol = this._semanticModel.GetSymbolInfo( node ).Symbol;

                if ( attributeSymbol is IMethodSymbol attributeConstructor && attributeConstructor.ContainingType.Name == nameof(TestLiveTemplateAttribute) )
                {
                    var parent = node.Parent?.Parent;

                    if ( parent == null )
                    {
                        return;
                    }

                    var parentSymbol = this._semanticModel.GetDeclaredSymbol( parent );

                    if ( parentSymbol == null )
                    {
                        return;
                    }

                    var attributeData =
                        parentSymbol.GetAttributes().Single( a => a.AttributeConstructor?.Equals( attributeConstructor ) == true );

                    var aspectType = (INamedTypeSymbol) attributeData.ConstructorArguments[0].Value!;

                    this._addTarget( (parentSymbol, aspectType) );
                }
            }
        }
    }
}