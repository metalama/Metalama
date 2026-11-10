// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Metalama.Framework.DesignTime.Contracts.Workspaces;
using Metalama.Framework.DesignTime.VisualStudio.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.InProcess;

/// <summary>
/// The implementation of <see cref="ICompilerServiceProvider"/> of a process that hosts both processes of the Visual Studio
/// design-time architecture. It serves the services of the Visual Studio user process, and <see cref="IWorkspaceReceiver"/>.
/// </summary>
/// <remarks>
/// The service provider of the user process does not serve <see cref="IWorkspaceReceiver"/> in Visual Studio, so the
/// workspace that the Visual Studio extension gives to it is not used there. In this mode, the analysis process has no
/// other way to get the workspace, so the interface is served by <see cref="VsInProcessAnalysisProcess"/>.
/// </remarks>
internal sealed class VsInProcessCompilerServiceProvider : VsUserProcessCompilerServiceProvider
{
    protected override ICompilerService? GetServiceCore( string name )
        => name switch
        {
            nameof(IWorkspaceReceiver) => this.ServiceProvider.GetRequiredService<VsInProcessAnalysisProcess>(),
            _ => base.GetServiceCore( name )
        };
}
