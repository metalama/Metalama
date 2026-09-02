// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
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

            if ( !testResult.Success )
            {
                return;
            }

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
                    // Sort the syntax trees by their generated content, and index them so that the test result file
                    // names stay short. The name is not usable as the sort key: the name of an introduced extension
                    // block is a hash of the path of the source file and of the position of the declaration in it, so
                    // it differs between the variant projects and between a deterministic and a non-deterministic
                    // build, which would make the index of each expected file differ as well.
                    var outputCompilation =
                        testResult.InputCompilation!.AddSyntaxTrees(
                            introducedSyntaxTrees.OrderBy( x => x.GeneratedSyntaxTree.ToString(), StringComparer.Ordinal )
                                .Select( ( x, i ) => x.GeneratedSyntaxTree.WithFilePath( $"{i}.cs" ) ) );

                    testResult.OutputCompilation = outputCompilation;
                    testResult.OutputCompilationDiagnostics.Report( outputCompilation.GetDiagnostics() );

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