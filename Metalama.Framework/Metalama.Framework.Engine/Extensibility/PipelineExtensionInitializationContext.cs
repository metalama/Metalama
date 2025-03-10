// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Extensibility;

public sealed class PipelineExtensionInitializationContext
{
    private readonly ImmutableArray<IDiagnosticDefinition>.Builder _diagnosticDefinitions = ImmutableArray.CreateBuilder<IDiagnosticDefinition>();
    private readonly ImmutableArray<SuppressionDefinition>.Builder _suppressionDefinitions = ImmutableArray.CreateBuilder<SuppressionDefinition>();

    public IProjectOptions ProjectOptions { get; }

    public IDiagnosticAdder Diagnostics { get; }

    public ServiceProviderBuilder<IProjectService> ServiceBuilder { get; } = new();

    public ProjectServiceProvider ServiceProvider { get; }

    internal PipelineExtensionInitializationContext(
        IProjectOptions projectOptions,
        IDiagnosticAdder diagnostics,
        ProjectServiceProvider serviceProvider )
    {
        this.ProjectOptions = projectOptions;
        this.Diagnostics = diagnostics;
        this.ServiceProvider = serviceProvider;
    }

    public void AddDiagnosticDefinitions( IEnumerable<IDiagnosticDefinition> diagnosticDefinitions )
        => this._diagnosticDefinitions.AddRange( diagnosticDefinitions );

    public void AddSuppressionDefinitions( IEnumerable<SuppressionDefinition> suppressionDefinitions )
        => this._suppressionDefinitions.AddRange( suppressionDefinitions );

    internal DiagnosticManifest DiagnosticManifest => new( this._diagnosticDefinitions.ToImmutable(), this._suppressionDefinitions.ToImmutable() );
}