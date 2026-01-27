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

    public static bool operator <( AdviceOrderingIndices left, AdviceOrderingIndices right ) => left.CompareTo( right ) < 0;

    public static bool operator >( AdviceOrderingIndices left, AdviceOrderingIndices right ) => left.CompareTo( right ) > 0;

    public static bool operator <=( AdviceOrderingIndices left, AdviceOrderingIndices right ) => left.CompareTo( right ) <= 0;

    public static bool operator >=( AdviceOrderingIndices left, AdviceOrderingIndices right ) => left.CompareTo( right ) >= 0;
}