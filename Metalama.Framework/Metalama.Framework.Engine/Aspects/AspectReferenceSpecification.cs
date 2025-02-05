// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Engine.Aspects
{
    /// <summary>
    /// Describes which version of the underlying semantic is referenced by the syntax node.
    /// </summary>
    internal readonly struct AspectReferenceSpecification
    {
        /// <summary>
        /// Gets the aspect layer which created the syntax node.
        /// </summary>
        private AspectLayerId AspectLayerId { get; }

        /// <summary>
        /// Gets a value indicating which version of the target semantic in relation to the aspect layer is referenced.
        /// </summary>
        public AspectReferenceOrder Order { get; }

        /// <summary>
        /// Gets a value indicating target kind. For example self or property get accessor.
        /// </summary>
        public AspectReferenceTargetKind TargetKind { get; }

        /// <summary>
        /// Gets a value indicating flags available to the linker on the reference.
        /// </summary>
        public AspectReferenceFlags Flags { get; }

        public AspectReferenceSpecification(
            AspectLayerId aspectLayerId,
            AspectReferenceOrder order,
            AspectReferenceTargetKind targetKind = AspectReferenceTargetKind.Self,
            AspectReferenceFlags flags = AspectReferenceFlags.None )
        {
            this.AspectLayerId = aspectLayerId;
            this.Order = order;
            this.TargetKind = targetKind;
            this.Flags = flags;
        }

        internal AspectReferenceSpecification WithTargetKind( AspectReferenceTargetKind targetKind )
        {
            return new AspectReferenceSpecification(
                this.AspectLayerId,
                this.Order,
                targetKind,
                this.Flags );
        }

        internal AspectReferenceSpecification WithOrder( AspectReferenceOrder order )
        {
            return new AspectReferenceSpecification( this.AspectLayerId, order, this.TargetKind, this.Flags );
        }

        public static AspectReferenceSpecification FromString( string str )
        {
            var parts = str.Split( '$' );

            var parseSuccess1 = Enum.TryParse<AspectReferenceOrder>( parts[1], out var order );

            Invariant.Assert( parseSuccess1 );

            var parseSuccess2 = Enum.TryParse<AspectReferenceTargetKind>( parts[2], out var targetKind );

            Invariant.Assert( parseSuccess2 );

            var parseSuccess3 = Enum.TryParse<AspectReferenceFlags>( parts[3], out var flags );

            Invariant.Assert( parseSuccess3 );

            return new AspectReferenceSpecification( AspectLayerId.FromString( parts[0] ), order, targetKind, flags );
        }

        public override string ToString()
        {
            // TODO: Cache strings.
            return $"{this.AspectLayerId.FullName}${this.Order}${this.TargetKind}${this.Flags}";
        }
    }
}