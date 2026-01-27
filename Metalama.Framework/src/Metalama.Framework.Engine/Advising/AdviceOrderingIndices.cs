// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Engine.Advising;

internal readonly record struct AdviceOrderingIndices(
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
}