// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Project;
using Microsoft.CodeAnalysis;
using SharpCrafters.Backstage.Telemetry;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Pipeline;

internal sealed class TransformerContextAdapter : ITransformerContext
{
    private readonly TransformerContext _underlying;

    public TransformerContextAdapter( TransformerContext underlying )
    {
        var globalServices = this.ServiceProvider.Underlying;

        this._underlying = underlying;

        // Try.Metalama ships its own project options factory using the async-local service provider.
        var projectOptionsFactory = globalServices.GetRequiredService<IProjectOptionsFactory>();

        this.ProjectOptions = projectOptionsFactory.GetProjectOptions(
            this._underlying.AnalyzerConfigOptionsProvider,
            this._underlying.Options );
    }

    public GlobalServiceProvider ServiceProvider { get; } = ServiceProviderFactory.GetServiceProvider();

    public void AddSyntaxTreeTransformations( IEnumerable<SyntaxTreeTransformation> transformations )
        => this._underlying.AddSyntaxTreeTransformations( transformations );

    public Compilation Compilation => this._underlying.Compilation;

    public IProjectOptions ProjectOptions { get; }

    /// <inheritdoc />
    /// <remarks>
    /// The context is registered by <see cref="SourceTransformer"/> in the service provider that it returns from
    /// <see cref="SourceTransformer.InitializeServices"/>, and Metalama.Compiler exposes that service provider through
    /// <see cref="TransformerContext.Services"/>. The context is resolved untyped, because it is a type of Backstage and not a
    /// service of the engine.
    /// </remarks>
    public ITelemetryContext? TelemetryContext => (ITelemetryContext?) this._underlying.Services?.GetService( typeof(ITelemetryContext) );

    public ImmutableArray<ManagedResource> Resources => this._underlying.Resources;

    public void ReportDiagnostic( Diagnostic diagnostic ) => this._underlying.ReportDiagnostic( diagnostic );

    public void AddResources( IEnumerable<ManagedResource> resources ) => this._underlying.AddResources( resources );

    public void RegisterDiagnosticFilter( in DiagnosticFilter filter ) => this._underlying.RegisterDiagnosticFilter( filter );
}