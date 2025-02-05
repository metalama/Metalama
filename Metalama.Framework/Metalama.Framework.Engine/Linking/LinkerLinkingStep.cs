// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Utilities.Threading;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Linking
{
    // Linking step does two things:
    //   * Transforms introduced code so that aspects use correct declarations that are consistent with aspect layer order.
    //   * When possible inlines code that is present in separate method after injection phase.
    //
    // For purposes of inlining, this step expects that code in all overrides have non-conflicting names. This should be managed in injection phase.
    //
    // For example if we have method A with overrides A1 and A2, the intermediate compilation contains three entities:
    //   * A, which is the original method.
    //   * A1, which contains annotated call to A.
    //   * A2, which contains annotated call to A.
    //
    // Non-inlined linked code (if no inlining is possible) contains:
    //   * A, which contains only a call A1.
    //   * A1, which replaces the annotated call with a call to A2.
    //   * A2, which replaced the annotated call with a call to Ao.
    //   * Ao, which contains the original method body.
    //
    // When inlining we need to know whether A1, A2 and Ao will be called from multiple places. This information is coming from analysis phase.
    //
    // Ideal inlining result is a single method A, which will contain logic from all aspects and the original method.

    /// <summary>
    /// Linker linking step, which rewrites the intermediate compilation and produces the final compilation. 
    /// </summary>
    internal sealed partial class LinkerLinkingStep : AspectLinkerPipelineStep<LinkerAnalysisStepOutput, AspectLinkerResult>
    {
        private readonly ProjectServiceProvider _serviceProvider;
        private readonly IConcurrentTaskRunner _concurrentTaskRunner;
        private readonly SyntaxGenerationOptions _syntaxGenerationOptions;

        public LinkerLinkingStep( in ProjectServiceProvider serviceProvider )
        {
            this._serviceProvider = serviceProvider;
            this._concurrentTaskRunner = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
            this._syntaxGenerationOptions = serviceProvider.GetRequiredService<SyntaxGenerationOptions>();
        }

        public override async Task<AspectLinkerResult> ExecuteAsync( LinkerAnalysisStepOutput input, CancellationToken cancellationToken )
        {
            var rewritingDriver = new LinkerRewritingDriver(
                this._serviceProvider,
                input.IntermediateCompilation.CompilationContext,
                input.InjectionRegistry,
                input.LateTransformationRegistry,
                input.AnalysisRegistry );

            ConcurrentQueue<SyntaxTreeTransformation> transformations = new();

            async Task ProcessTransformationAsync( SyntaxTreeTransformation modifiedSyntaxTree )
            {
                if ( modifiedSyntaxTree is { Kind: SyntaxTreeTransformationKind.Add, FilePath: LinkerInjectionHelperProvider.SyntaxTreeName } )
                {
                    // This is an intermediate tree we added and we don't need it in the final compilation.
                    transformations.Enqueue( SyntaxTreeTransformation.RemoveTree( modifiedSyntaxTree.NewTree.AssertNotNull() ) );
                }
                else
                {
                    var syntaxTree = modifiedSyntaxTree.NewTree.AssertNotNull();

                    if ( !input.IntermediateCompilation.IsSyntaxTreeObserved( syntaxTree.FilePath ) )
                    {
                        return;
                    }

                    // Run the linking rewriter for this tree.
                    var linkedRoot =
                        new LinkingRewriter( input.IntermediateCompilation.CompilationContext, rewritingDriver )
                            .Visit( await syntaxTree.GetRootAsync( cancellationToken ) )!;

                    SyntaxGenerationContext syntaxGenerationContext;

                    if ( modifiedSyntaxTree.OldTree == null )
                    {
                        syntaxGenerationContext = input.SourceCompilationModel.CompilationContext.GetSyntaxGenerationContext(
                            this._syntaxGenerationOptions,
                            false,
                            false,
                            "\n" );
                    }
                    else
                    {
                        syntaxGenerationContext = input.SourceCompilationModel.CompilationContext.GetSyntaxGenerationContext(
                            this._syntaxGenerationOptions,
                            modifiedSyntaxTree.OldTree!,
                            0 );
                    }

                    var cleanRoot =
                        new CleanupRewriter(
                                input.ProjectOptions,
                                syntaxGenerationContext )
                            .Visit( linkedRoot )!;

                    var fixedRoot = PreprocessorFixer.Fix( cleanRoot, syntaxGenerationContext );

                    var newSyntaxTree = syntaxTree.WithRootAndOptions( fixedRoot, syntaxTree.Options );

                    transformations.Enqueue( SyntaxTreeTransformation.ReplaceTree( syntaxTree, newSyntaxTree ) );
                }
            }

            await this._concurrentTaskRunner.RunConcurrentlyAsync(
                input.IntermediateCompilation.ModifiedSyntaxTrees.Values,
                ProcessTransformationAsync,
                cancellationToken );

            var linkedCompilation =
                input.IntermediateCompilation
                    .Update( transformations );

            return new AspectLinkerResult( linkedCompilation, input.DiagnosticSink.ToImmutable() );
        }
    }
}