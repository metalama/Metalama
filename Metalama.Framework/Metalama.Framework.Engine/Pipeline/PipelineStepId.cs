// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using System;

namespace Metalama.Framework.Engine.Pipeline
{
    /// <summary>
    /// The identifier of a <see cref="PipelineStepId"/>. For inequality comparison, see <see cref="PipelineStepIdComparer"/>.
    /// </summary>
    internal readonly struct PipelineStepId : IEquatable<PipelineStepId>
    {
        public AspectLayerId AspectLayerId { get; }

        public int AspectTargetTypeDepth { get; }

        public int AspectTargetDepth { get; }

        public int AdviceTargetDepth { get; }

        public PipelineStepId( AspectLayerId aspectLayerId, int aspectTargetTypeDepth, int aspectTargetDepth, int adviceTargetDepth )
        {
            this.AspectLayerId = aspectLayerId;
            this.AspectTargetTypeDepth = aspectTargetTypeDepth;
            this.AspectTargetDepth = aspectTargetDepth;
            this.AdviceTargetDepth = adviceTargetDepth;
        }

        public bool Equals( PipelineStepId other )
            => this.AspectLayerId.Equals( other.AspectLayerId ) && this.AspectTargetTypeDepth == other.AspectTargetTypeDepth
                                                                && this.AspectTargetDepth == other.AspectTargetDepth
                                                                && this.AdviceTargetDepth == other.AdviceTargetDepth;

        public override bool Equals( object? obj ) => obj is PipelineStepId other && this.Equals( other );

        public override int GetHashCode() => HashCode.Combine( this.AspectLayerId, this.AspectTargetTypeDepth, this.AspectTargetDepth, this.AdviceTargetDepth );

        public override string ToString() => $"{this.AspectLayerId}:{this.AspectTargetTypeDepth}:{this.AspectTargetDepth}:{this.AdviceTargetDepth}";
    }
}