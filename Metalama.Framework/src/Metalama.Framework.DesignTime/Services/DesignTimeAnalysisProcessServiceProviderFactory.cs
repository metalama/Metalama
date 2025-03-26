// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Utilities;
using Metalama.Framework.DesignTime.AspectExplorer;
using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;

namespace Metalama.Framework.DesignTime.Services;

internal class DesignTimeAnalysisProcessServiceProviderFactory : DesignTimeServiceProviderFactory
{
    public DesignTimeAnalysisProcessServiceProviderFactory() : this( null ) { }

    protected DesignTimeAnalysisProcessServiceProviderFactory( DesignTimeEntryPointManager? entryPointManager, DesignTimeProcessKind processKind ) : base(
        entryPointManager,
        processKind ) { }

    public DesignTimeAnalysisProcessServiceProviderFactory( DesignTimeEntryPointManager? entryPointManager ) : base(
        entryPointManager,
        DesignTimeProcessKind.Default ) { }

    private static WorkspaceProvider GetWorkspaceProvider( GlobalServiceProvider serviceProvider )
    {
        // TODO: WorkspaceProvider should be refactored as an IGlobalService.

        switch ( ProcessUtilities.ProcessKind )
        {
            case ProcessKind.Rider:
            case ProcessKind.OmniSharp:
            case ProcessKind.VisualStudioMac:
            case ProcessKind.LanguageServer:
                return new LocalWorkspaceProvider( serviceProvider );

            default:
                if ( RemoteWorkspaceProvider.TryCreate( serviceProvider, out var workspaceProvider ) )
                {
                    return workspaceProvider;
                }
                else
                {
                    // This is used in tests, when we test the DesignTimeServiceProviderFactory class.
                    return new FakeWorkspaceProvider( serviceProvider );
                }
        }
    }

    protected override ServiceProvider<IGlobalService> AddServices( ServiceProvider<IGlobalService> serviceProvider )
    {
        // Initialize exception reporter.
        serviceProvider = base.AddServices( serviceProvider );

        // Initialize the event hub.
        serviceProvider = serviceProvider
            .WithService( sp => new AnalysisProcessEventHub( sp ) );

        serviceProvider = serviceProvider
            .WithService( sp => GetWorkspaceProvider( sp ) )
            .WithService( sp => new DesignTimeInvalidationService( sp ) );

        // Add the pipeline factory.
        serviceProvider = serviceProvider.WithService( sp => new DesignTimeAspectPipelineFactory( sp ) );

        // Add services that depend on the pipeline factory.
        serviceProvider = serviceProvider.WithService( sp => new AspectDatabase( sp ) );

        return serviceProvider;
    }
}