// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.SourceGeneration;

/// <summary>
/// An implementation of <see cref="BaseSourceGenerator"/> that should execute in the analysis process.
/// </summary>
[UsedImplicitly]
[PublicAPI]
public class AnalysisProcessSourceGenerator : BaseSourceGenerator
{
    private protected override ProjectSourceGenerator CreateSourceGeneratorImpl( IProjectOptions projectOptions, ProjectKey projectKey )
        => new AnalysisProcessProjectSourceGenerator( this.ServiceProvider, projectOptions, projectKey );

    private protected override void OnGeneratedSourceRequested(
        Compilation compilation,
        IProjectOptions options,
        TestableCancellationToken cancellationToken )
    {
        // If there is a cached compilation result, this will schedule a background computation of the compilation even if the TouchId is unchanged.
        // If there is no cached result, this will perform a synchronous computation and the next call will return it from cache.

        _ = this.GetGeneratedSources( compilation, options, cancellationToken );
    }

    // This constructor is called by the facade.
    [UsedImplicitly]
    public AnalysisProcessSourceGenerator() { }

    public AnalysisProcessSourceGenerator( ServiceProvider<IGlobalService> serviceProvider ) : base( serviceProvider ) { }
}