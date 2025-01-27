// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Validation;
using System.Collections.Immutable;
using System.Threading;

namespace Metalama.Framework.Engine.Aspects;

internal sealed class AspectBuilderState
{
    private readonly AdviceFactoryState _adviceFactoryState;
    private readonly ObjectReaderFactory _objectReaderFactory;
    private readonly IObjectReader _defaultTagReader;

    public ProjectServiceProvider ServiceProvider { get; }

    public UserDiagnosticSink Diagnostics { get; }

    public AspectPipelineConfiguration Configuration { get; }

    public ImmutableArray<IAspectSource> AspectSources { get; set; } = ImmutableArray<IAspectSource>.Empty;

    public ImmutableArray<IValidatorSource> ValidatorSources { get; set; } = ImmutableArray<IValidatorSource>.Empty;

    public ImmutableArray<IHierarchicalOptionsSource> OptionsSources { get; set; } = ImmutableArray<IHierarchicalOptionsSource>.Empty;

    public CancellationToken CancellationToken { get; }

    public IAspectInstanceInternal AspectInstance { get; }

    public string? Layer { get; }

    public object? Tags { get; set; }

    public AspectBuilderState(
        ProjectServiceProvider serviceProvider,
        UserDiagnosticSink diagnostics,
        AspectPipelineConfiguration configuration,
        IAspectInstanceInternal aspectInstance,
        AdviceFactoryState adviceFactoryState,
        string? layer,
        CancellationToken cancellationToken )
    {
        this.ServiceProvider = serviceProvider;
        this.Diagnostics = diagnostics;
        this.Configuration = configuration;
        this.CancellationToken = cancellationToken;
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
                this.AspectSources,
                this.ValidatorSources,
                this.OptionsSources )
            : new AspectInstanceResult(
                this.AspectInstance,
                outcome,
                this.Diagnostics.ToImmutable(),
                ImmutableArray<ITransformation>.Empty,
                ImmutableArray<IAspectSource>.Empty,
                ImmutableArray<IValidatorSource>.Empty,
                ImmutableArray<IHierarchicalOptionsSource>.Empty );
    }
}