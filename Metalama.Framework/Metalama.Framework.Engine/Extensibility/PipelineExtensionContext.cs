// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;

namespace Metalama.Framework.Engine.Extensibility;

public sealed class PipelineExtensionContext
{
    public IProjectOptions ProjectOptions { get; }

    public IDiagnosticAdder Diagnostics { get; }

    public ServiceProviderBuilder<IProjectService> ServiceBuilder { get; } = new();
    
    public ProjectServiceProvider ServiceProvider { get; }

    internal PipelineExtensionContext(
        IProjectOptions projectOptions,
        IDiagnosticAdder diagnostics,
        ProjectServiceProvider serviceProvider )
    {
        this.ProjectOptions = projectOptions;
        this.Diagnostics = diagnostics;
        this.ServiceProvider = serviceProvider;
    }
}