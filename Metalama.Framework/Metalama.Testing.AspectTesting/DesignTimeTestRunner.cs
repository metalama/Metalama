// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Metalama.Testing.AspectTesting
{
    internal sealed class DesignTimeTestRunner : BaseTestRunner
    {
        public DesignTimeTestRunner(
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
            await base.RunAsync( testInput, testResult, testContext );

            using var pipeline = new TestDesignTimeAspectPipeline( testContext.ServiceProvider );

            var pipelineResult = await pipeline.ExecuteAsync( testResult.InputCompilation! );

            testResult.PipelineDiagnostics.Report( pipelineResult.Diagnostics );

            if ( pipelineResult.Success )
            {
                testResult.HasOutputCode = true;

                var introducedSyntaxTrees = pipelineResult.AdditionalSyntaxTrees;

                testResult.AddDiagnosticSuppressions( pipelineResult.Suppressions );

                if ( introducedSyntaxTrees.Length > 0 )
                {
                    // Sort syntax trees by name.
                    // Since the syntax tree name includes the full name of the type, we must index them to avoid too long test result file names.
                    // TODO: Underlying names may not be deterministic, which makes this non-deterministic too.
                    var outputCompilation =
                        testResult.InputCompilation!.AddSyntaxTrees(
                            introducedSyntaxTrees.OrderBy( x => x.Name, StringComparer.Ordinal )
                                .Select( ( x, i ) => x.GeneratedSyntaxTree.WithFilePath( $"{i}.cs" ) ) );

                    testResult.OutputCompilation = outputCompilation;
                    testResult.OutputCompilationDiagnostics.Report( outputCompilation.GetDiagnostics().Where( d => d.Severity == DiagnosticSeverity.Error ) );

                    await testResult.SetOutputCompilationAsync( outputCompilation );
                }
                else
                {
                    testResult.OutputCompilation = testResult.InputCompilation;
                }
            }
            else
            {
                testResult.SetFailed( "DesignTimeAspectPipeline.TryExecute failed" );
            }
        }
    }
}