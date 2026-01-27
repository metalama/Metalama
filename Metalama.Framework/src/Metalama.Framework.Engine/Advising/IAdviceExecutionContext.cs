// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Introspection;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Transformations;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Advising;

internal interface IAdviceExecutionContext
{
    CompilationModel MutableCompilation { get; }

    IAspectInstanceInternal AspectInstance { get; }

    ref readonly ProjectServiceProvider ServiceProvider { get; }

    IDiagnosticAdder Diagnostics { get; }

    IntrospectionPipelineListener? IntrospectionListener { get; }

    void AddTransformations( ImmutableArray<ITransformation> transformations );

    void AddTransitiveAspects( ImmutableArray<TransitiveAspectInstance> aspects );

    /// <summary>
    /// Gets the ordering values for the next transformation and increments the counter.
    /// </summary>
    AdviceOrderingIndices GetAdviceOrderIndices();

    int AspectOrder { get; }

    IAspectClassResolver AspectClassResolver { get; }
}

internal record struct AdviceOrderingIndices(
    int OrderWithinPipeline,
    int OrderWithinPipelineStepAndType,
    int OrderWithinPipelineStepAndTypeAndAspectInstance ) : IComparable<AdviceOrderingIndices>
{
    public int CompareTo( AdviceOrderingIndices other ) => this.CompareToCore( other );

    public int CompareTo( in AdviceOrderingIndices other ) => this.CompareToCore( other );

    private int CompareToCore( in AdviceOrderingIndices other )
    {
        var orderWithinPipelineComparison = this.OrderWithinPipeline.CompareTo( other.OrderWithinPipeline );

        if ( orderWithinPipelineComparison != 0 )
        {
            return orderWithinPipelineComparison;
        }

        var orderWithinPipelineStepAndTypeComparison = this.OrderWithinPipelineStepAndType.CompareTo( other.OrderWithinPipelineStepAndType );

        if ( orderWithinPipelineStepAndTypeComparison != 0 )
        {
            return orderWithinPipelineStepAndTypeComparison;
        }

        return this.OrderWithinPipelineStepAndTypeAndAspectInstance.CompareTo( other.OrderWithinPipelineStepAndTypeAndAspectInstance );
    }

    public static bool operator <( AdviceOrderingIndices left, AdviceOrderingIndices right ) => left.CompareTo( right ) < 0;

    public static bool operator >( AdviceOrderingIndices left, AdviceOrderingIndices right ) => left.CompareTo( right ) > 0;

    public static bool operator <=( AdviceOrderingIndices left, AdviceOrderingIndices right ) => left.CompareTo( right ) <= 0;

    public static bool operator >=( AdviceOrderingIndices left, AdviceOrderingIndices right ) => left.CompareTo( right ) >= 0;
}