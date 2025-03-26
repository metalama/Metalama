// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline.DesignTime;

public sealed class TestDesignTimeAspectPipeline : BaseDesignTimeAspectPipeline
{
    public TestDesignTimeAspectPipeline( in ProjectServiceProvider serviceProvider ) : base( serviceProvider ) { }

    public async Task<TestDesignTimeAspectPipelineResult> ExecuteAsync( Compilation inputCompilation )
    {
        var diagnosticList = new DiagnosticBag();

        var partialCompilation = PartialCompilation.CreateComplete( inputCompilation );

        // Run the validator, which is run in design-time and may crash on invalid code.
        foreach ( var syntaxTree in partialCompilation.SyntaxTrees.Values )
        {
            var semanticModel = partialCompilation.Compilation.GetSemanticModel( syntaxTree );

            TemplatingCodeValidator.Validate(
                this.ServiceProvider,
                semanticModel,
                diagnosticList.Report,
                null,
                false,
                true,
                this.ApplicationExitingToken );
        }

        if ( !this.TryInitialize( diagnosticList, partialCompilation.Compilation, null, this.ApplicationExitingToken, out var configuration ) )
        {
            return new TestDesignTimeAspectPipelineResult(
                false,
                diagnosticList.ToImmutableArray(),
                ImmutableArray<ScopedSuppression>.Empty,
                ImmutableArray<IntroducedSyntaxTree>.Empty );
        }

        // Inject a DependencyCollector so we can test exceptions based on its presence.
        configuration = configuration.WithServiceProvider( configuration.ServiceProvider.Underlying.WithService( new DependencyCollector() ) );

        var stageResult = await this.ExecuteAsync( partialCompilation, diagnosticList, configuration, TestableCancellationToken.None );

        if ( !stageResult.IsSuccessful )
        {
            return new TestDesignTimeAspectPipelineResult(
                false,
                diagnosticList.ToImmutableArray(),
                ImmutableArray<ScopedSuppression>.Empty,
                ImmutableArray<IntroducedSyntaxTree>.Empty );
        }

        return new TestDesignTimeAspectPipelineResult(
            true,
            stageResult.Value.Diagnostics.ReportedDiagnostics,
            stageResult.Value.Diagnostics.DiagnosticSuppressions,
            stageResult.Value.AdditionalSyntaxTrees );
    }

    private sealed class DependencyCollector : IDependencyCollector
    {
        public void AddDependency( INamedTypeSymbol masterSymbol, INamedTypeSymbol dependentSymbol ) { }

        public void AddDependency( INamedTypeSymbol masterSymbol, SyntaxTree dependentTree ) { }

        public void AddDependency( SyntaxTree masterTree, SyntaxTree dependentTree ) { }
    }
}