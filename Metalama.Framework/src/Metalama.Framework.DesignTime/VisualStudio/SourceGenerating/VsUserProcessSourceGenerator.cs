// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.SourceGeneration;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;

[UsedImplicitly]
[PublicAPI]
public sealed class VsUserProcessSourceGenerator : BaseSourceGenerator
{
    private protected override ProjectSourceGenerator CreateSourceGeneratorImpl( IProjectOptions projectOptions, ProjectKey projectKey )
        => new VsUserProcessProjectSourceGenerator( this.ServiceProvider, projectOptions, projectKey );

    private protected override void OnGeneratedSourceRequested( Compilation compilation, IProjectOptions options, TestableCancellationToken cancellationToken )
    {
        // In the DevEnv process, we always serve from cache because the initiator of the source generator pipeline is always a change in the touch file
        // done by the analysis process, and this change is done after the devenv process receives the generated code from the named pipe.
    }

    // This constructor is called by the facade.
    public VsUserProcessSourceGenerator() { }

    internal VsUserProcessSourceGenerator( ServiceProvider<IGlobalService> serviceProvider ) : base( serviceProvider ) { }
}