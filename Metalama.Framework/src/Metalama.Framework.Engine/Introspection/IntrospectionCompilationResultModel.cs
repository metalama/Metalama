// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Introspection;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Introspection;

internal sealed class IntrospectionCompilationResultModel : IIntrospectionCompilationResult
{
    private readonly IIntrospectionOptionsProvider _options;
    private readonly AspectPipelineResult? _pipelineResult;
    private readonly CompilationModel _compilation;
    private readonly IntrospectionFactory _factory;

    public IntrospectionCompilationResultModel(
        string name,
        IIntrospectionOptionsProvider? options,
        bool hasSucceeded,
        CompilationModel compilationModel,
        ImmutableArray<IIntrospectionDiagnostic> diagnostics,
        IntrospectionFactory factory,
        AspectPipelineResult? pipelineResult = null )
    {
        this._options = options ?? new DefaultIntrospectionOptionsProvider();
        this._pipelineResult = pipelineResult;
        this.Name = name;
        this.HasMetalamaSucceeded = hasSucceeded;
        this.Diagnostics = diagnostics;
        this._factory = factory;
        this._compilation = compilationModel;
    }

    public string Name { get; }

    public bool HasMetalamaSucceeded { get; }

    public ImmutableArray<IIntrospectionDiagnostic> Diagnostics { get; }

    public ImmutableArray<IIntrospectionAspectLayer> AspectLayers => this.GetAspectLayersCore();

    private ImmutableArray<IIntrospectionAspectLayer> GetAspectLayersCore()
    {
        if ( this._pipelineResult == null )
        {
            if ( this._options.IntrospectionOptions.IgnoreErrors )
            {
                return ImmutableArray<IIntrospectionAspectLayer>.Empty;
            }
            else
            {
                throw this.CreateCompilationFailedException();
            }
        }

        // This has the side effect of creating all aspect classes and caching them in the property.
        if ( this.AspectClasses.IsDefaultOrEmpty )
        {
            return ImmutableArray<IIntrospectionAspectLayer>.Empty;
        }

        return this._pipelineResult.AspectLayers.Select( x => new IntrospectionAspectLayer( x, this._factory ) )
            .ToImmutableArray<IIntrospectionAspectLayer>();
    }

    [Memo]
    public ImmutableArray<IIntrospectionAspectInstance> AspectInstances => this.AspectClasses.SelectMany( x => x.Instances ).ToImmutableArray();

    private CompilationFailedException CreateCompilationFailedException()
        => new(
            $"The compilation of the project '{this._compilation.Name}' failed. Check the Diagnostics collection for details. Use WithIgnoreErrors(true) to ignore errors.",
            this.Diagnostics.Where( d => d.Severity is Severity.Error or Severity.Warning ).ToImmutableArray() );

    [Memo]
    public ImmutableArray<IIntrospectionAspectClass> AspectClasses => this.GetAspectClassesCore();

    private ImmutableArray<IIntrospectionAspectClass> GetAspectClassesCore()
    {
        if ( this._pipelineResult == null )
        {
            if ( this._options.IntrospectionOptions.IgnoreErrors )
            {
                return ImmutableArray<IIntrospectionAspectClass>.Empty;
            }
            else
            {
                throw this.CreateCompilationFailedException();
            }
        }

        var aspectInstancesByClass = this._pipelineResult.AspectInstanceResults.GroupBy( x => x.AspectInstance.AspectClass )
            .ToDictionary( x => x.Key, x => x.ToImmutableArray() );

        return
            this._pipelineResult.AspectLayers.Select( l => l.AspectClass )
                .Distinct()
                .Select(
                    x => this._factory.CreateIntrospectionAspectClass(
                        x,
                        aspectInstancesByClass.TryGetValue( x, out var instances ) ? instances : ImmutableArray<AspectInstanceResult>.Empty ) )
                .ToImmutableArray();
    }

    [Memo]
    public ImmutableArray<IIntrospectionAdvice> Advice => this.AspectInstances.SelectMany( i => i.Advice ).ToImmutableArray();

    [Memo]
    public ImmutableArray<IIntrospectionTransformation> Transformations => this.Advice.SelectMany( i => i.Transformations ).ToImmutableArray();

    public ICompilation TransformedCode
    {
        get
        {
            if ( this._pipelineResult == null && !this._options.IntrospectionOptions.IgnoreErrors )
            {
                throw this.CreateCompilationFailedException();
            }

            return this._compilation;
        }
    }

    public bool IsMetalamaEnabled => true;
}