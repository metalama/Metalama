// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Advising;

internal sealed class AdviceImplementationContext
{
    private readonly IAdviceExecutionContext _adviceExecutionContext;

    private ImmutableArray<ITransformation>.Builder? _transformations;
    private ImmutableArray<TransitiveAspectInstance>.Builder? _transitiveAspects;

    public AdviceImplementationContext( DiagnosticBag diagnostics, IAdviceExecutionContext adviceExecutionContext )
    {
        this.Diagnostics = diagnostics;
        this._adviceExecutionContext = adviceExecutionContext;
    }

    public void ThrowIfAnyError()
    {
        if ( this.Diagnostics.HasError() )
        {
            throw new DiagnosticException(
                "Errors have occurred while creating advice.",
                this.Diagnostics.Where( d => d.Severity == DiagnosticSeverity.Error ).ToImmutableArray() );
        }
    }

    public CompilationModel MutableCompilation => this._adviceExecutionContext.MutableCompilation;

    public ProjectServiceProvider ServiceProvider => this._adviceExecutionContext.ServiceProvider;

    public DiagnosticBag Diagnostics { get; }

    public void AddTransformation( ITransformation transformation )
    {
        transformation.SetAdviceOrderingIndices( this._adviceExecutionContext.GetNextAdviceOrderIndices() );
        this.AddTransformationWithoutSettingOrders( transformation );
    }

    /// <summary>
    /// Gets the ordering indices for the next transformation. This method has a side effect: it increments the internal
    /// transformation counter, so each call returns a unique set of indices. Do not call this method more than once
    /// per transformation.
    /// </summary>
    public AdviceOrderingIndices GetNextAdviceOrderIndices() => this._adviceExecutionContext.GetNextAdviceOrderIndices();

    public void AddTransformationWithoutSettingOrders( ITransformation transformation )
    {
        this._transformations ??= ImmutableArray.CreateBuilder<ITransformation>();
        this._transformations.Add( transformation );
    }

    public void AddTransitiveAspect( TransitiveAspectInstance aspect )
    {
        this._transitiveAspects ??= ImmutableArray.CreateBuilder<TransitiveAspectInstance>();
        this._transitiveAspects.Add( aspect );
    }

    public ImmutableArray<TransitiveAspectInstance> TransitiveAspects
        => this._transitiveAspects?.ToImmutable() ?? ImmutableArray<TransitiveAspectInstance>.Empty;

    public ImmutableArray<ITransformation> Transformations => this._transformations?.ToImmutable() ?? ImmutableArray<ITransformation>.Empty;

    public int AspectOrder => this._adviceExecutionContext.AspectOrder;

    public IAspectClassResolver AspectClassResolver => this._adviceExecutionContext.AspectClassResolver;
}