// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.UserCode;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Metalama.Framework.Engine.Aspects;

internal sealed class AspectBuilderState : IPipelineContributorCollector
{
    private readonly AdviceFactoryState _adviceFactoryState;
    private readonly ObjectReaderFactory _objectReaderFactory;
    private readonly IObjectReader _defaultTagReader;
    private List<IPipelineContributor>? _pipelineContributors;

    public ProjectServiceProvider ServiceProvider { get; }

    public UserDiagnosticSink Diagnostics { get; }

    public AspectPipelineConfiguration Configuration { get; }

    public CancellationToken CancellationToken { get; }

    public IAspectInstanceInternal AspectInstance { get; }

    public UserCodeExecutionContext UserCodeExecutionContext { get; }

    public string? Layer { get; }

    public object? Tags { get; set; }

    public AspectBuilderState(
        ProjectServiceProvider serviceProvider,
        UserDiagnosticSink diagnostics,
        AspectPipelineConfiguration configuration,
        IAspectInstanceInternal aspectInstance,
        AdviceFactoryState adviceFactoryState,
        string? layer,
        UserCodeExecutionContext userCodeExecutionContext,
        CancellationToken cancellationToken )
    {
        this.ServiceProvider = serviceProvider;
        this.Diagnostics = diagnostics;
        this.Configuration = configuration;
        this.CancellationToken = cancellationToken;
        this.UserCodeExecutionContext = userCodeExecutionContext;
        this.AspectInstance = aspectInstance;
        this.CancellationToken = cancellationToken;
        this._adviceFactoryState = adviceFactoryState;
        this.Layer = layer;
        this._objectReaderFactory = serviceProvider.Global.GetRequiredService<ObjectReaderFactory>();
        this._defaultTagReader = this._objectReaderFactory.GetLazyReader( serviceProvider, null, () => this.Tags );
    }

    internal IObjectReader GetTagsReader( object? tags )
    {
        if ( tags == null )
        {
            return this._defaultTagReader;
        }
        else
        {
            return this._objectReaderFactory.GetLazyReader( this.ServiceProvider, tags, () => this.Tags );
        }
    }

    internal AspectInstanceResult ToResult()
    {
        var outcome = this.Diagnostics.ErrorCount == 0 ? this.AspectInstance.IsSkipped ? AdviceOutcome.Ignore : AdviceOutcome.Default : AdviceOutcome.Error;

        return outcome == AdviceOutcome.Default
            ? new AspectInstanceResult(
                this.AspectInstance,
                outcome,
                this.Diagnostics.ToImmutable(),
                this._adviceFactoryState.Transformations.ToImmutableArray(),
                this._pipelineContributors?.ToImmutableArray() ?? ImmutableArray<IPipelineContributor>.Empty )
            : new AspectInstanceResult(
                this.AspectInstance,
                outcome,
                this.Diagnostics.ToImmutable(),
                ImmutableArray<ITransformation>.Empty,
                ImmutableArray<IPipelineContributor>.Empty );
    }

    public void AddContributor( IPipelineContributor contributor )
    {
        this._pipelineContributors ??= new List<IPipelineContributor>();
        this._pipelineContributors.Add( contributor );
    }
}