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

internal class AdviceImplementationContext
{
    private readonly IAdviceExecutionContext _adviceExecutionContext;

    private ImmutableArray<ITransformation>.Builder? _transformations;
    private ImmutableArray<ITransitiveAspect>.Builder? _transitiveAspects;

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
                "Errors have occured while creating advice.",
                this.Diagnostics.Where( d => d.Severity == DiagnosticSeverity.Error ).ToImmutableArray() );
        }
    }

    public CompilationModel MutableCompilation => this._adviceExecutionContext.MutableCompilation;

    public ProjectServiceProvider ServiceProvider => this._adviceExecutionContext.ServiceProvider;

    public DiagnosticBag Diagnostics { get; }

    public void AddTransformation( ITransformation transformation )
    {
        this._adviceExecutionContext.SetOrders( transformation );
        this._transformations ??= ImmutableArray.CreateBuilder<ITransformation>();
        this._transformations.Add( transformation );
    }

    public void AddTransitiveAspect( ITransitiveAspect aspect )
    {
        this._transitiveAspects ??= ImmutableArray.CreateBuilder<ITransitiveAspect>();
        this._transitiveAspects.Add( aspect );
    }

    public ImmutableArray<ITransitiveAspect> TransitiveAspects => this._transitiveAspects?.ToImmutable() ?? ImmutableArray<ITransitiveAspect>.Empty;

    public ImmutableArray<ITransformation> Transformations => this._transformations?.ToImmutable() ?? ImmutableArray<ITransformation>.Empty;

    public int AspectOrder => this._adviceExecutionContext.AspectOrder;
}