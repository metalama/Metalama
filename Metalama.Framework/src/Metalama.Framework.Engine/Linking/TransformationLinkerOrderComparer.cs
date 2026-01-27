// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Transformations;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Linking;

internal sealed class TransformationLinkerOrderComparer : Comparer<ITransformation>
{
    public static TransformationLinkerOrderComparer Instance { get; } = new();

    private TransformationLinkerOrderComparer() { }

    public override int Compare( ITransformation? x, ITransformation? y )
    {
        if ( x == y )
        {
            return 0;
        }

        if ( x == null )
        {
            return 1;
        }
        else if ( y == null )
        {
            return -1;
        }

        // Sort by order within the pipeline, then type, then aspect instance (i.e. the order in which the advice were added).
        var adviceOrderingComparison = x.AdviceOrderingIndices.CompareTo( y.AdviceOrderingIndices );

        if ( adviceOrderingComparison != 0 )
        {
            return adviceOrderingComparison;
        }

        // There may still be some non-determinism at this point because types of the same depth are not ordered,
        // however this non-determinism should not affect the output of the linker.
        return 0;
    }
}