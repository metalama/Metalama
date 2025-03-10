// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.VersionNeutral;
using Metalama.Framework.DesignTime.VisualStudio.ServiceHub;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.Services;

internal sealed class VsUserProcessServiceProviderFactory : DesignTimeUserProcessServiceProviderFactory
{
    [UsedImplicitly]
    public VsUserProcessServiceProviderFactory() : this( null ) { }

    public VsUserProcessServiceProviderFactory( DesignTimeEntryPointManager? entryPointManager ) : base( entryPointManager ) { }

    protected override CompilerServiceProvider CreateCompilerServiceProvider() => new VsUserProcessCompilerServiceProvider();

    protected override ServiceProvider<IGlobalService> AddServices( ServiceProvider<IGlobalService> serviceProvider )
        => base.AddServices( serviceProvider )
            .WithService( sp => ServiceHubServerEndpoint.GetInstance( sp ) )
            .WithService( sp => new LocalWorkspaceProvider( sp ) );
}