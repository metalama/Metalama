// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
        public AspectLayerId AspectLayerId { get; }

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

        /// <summary>
        /// Gets the documentation comment ID of the target declaration, or <c>null</c> if the target declaration is not known.
        /// When set, this allows the linker to resolve the referenced symbol through <c>Compilation.Assembly</c> instead of
        /// the slower <c>SemanticModel.GetSymbolInfo</c>.
        /// </summary>
        public string? TargetDeclarationId { get; }

        public AspectReferenceSpecification(
            AspectLayerId aspectLayerId,
            AspectReferenceOrder order,
            AspectReferenceTargetKind targetKind = AspectReferenceTargetKind.Self,
            AspectReferenceFlags flags = AspectReferenceFlags.None,
            string? targetDeclarationId = null )
        {
            this.AspectLayerId = aspectLayerId;
            this.Order = order;
            this.TargetKind = targetKind;
            this.Flags = flags;
            this.TargetDeclarationId = targetDeclarationId;
        }

        internal AspectReferenceSpecification WithTargetKind( AspectReferenceTargetKind targetKind )
        {
            return new AspectReferenceSpecification(
                this.AspectLayerId,
                this.Order,
                targetKind,
                this.Flags,
                this.TargetDeclarationId );
        }

        internal AspectReferenceSpecification WithOrder( AspectReferenceOrder order )
        {
            return new AspectReferenceSpecification( this.AspectLayerId, order, this.TargetKind, this.Flags, this.TargetDeclarationId );
        }

        internal AspectReferenceSpecification WithTargetDeclarationId( string? targetDeclarationId )
        {
            return new AspectReferenceSpecification( this.AspectLayerId, this.Order, this.TargetKind, this.Flags, targetDeclarationId );
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

            var targetDeclarationId = parts.Length > 4 && parts[4].Length > 0 ? parts[4] : (string?) null;

            return new AspectReferenceSpecification( AspectLayerId.FromString( parts[0] ), order, targetKind, flags, targetDeclarationId );
        }

        public override string ToString()
        {
            // TODO: Cache strings.
            return $"{this.AspectLayerId.FullName}${this.Order}${this.TargetKind}${this.Flags}${this.TargetDeclarationId}";
        }
    }
}
