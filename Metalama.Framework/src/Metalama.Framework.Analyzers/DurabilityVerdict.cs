// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Analyzers;
using System.Collections.Immutable;

namespace Metalama.Framework.Analyzers.Durability
{
    /// <summary>
    /// The result of evaluating the durability of a type, with the chain of members and types that explains a
    /// negative verdict.
    /// </summary>
    /// <remarks>
    /// The chain holds strings and never symbols. A diagnostic is retained by Roslyn for as long as the compilation
    /// that produced it, so an analyzer that reported a symbol as a message argument would keep that symbol alive.
    /// That is the discipline this analyzer exists to enforce, and it applies to the analyzer itself.
    /// </remarks>
    internal sealed class DurabilityVerdict
    {
        /// <summary>
        /// The verdict of a type that is durable. A singleton, because it is by far the most frequent result and it
        /// carries no chain.
        /// </summary>
        public static readonly DurabilityVerdict Durable = new( DurabilityKind.Durable, ImmutableArray<string>.Empty, null );

        /// <summary>
        /// Gets the kind of the verdict.
        /// </summary>
        public DurabilityKind Kind { get; }

        /// <summary>
        /// Gets the chain that leads from the type being evaluated to the one responsible for a negative verdict,
        /// innermost last.
        /// </summary>
        public ImmutableArray<string> Chain { get; }

        /// <summary>
        /// Gets the explanation appended after the chain, or <c>null</c>.
        /// </summary>
        public string? Reason { get; }

        /// <summary>
        /// Gets a value indicating whether the type is durable.
        /// </summary>
        public bool IsDurable => this.Kind == DurabilityKind.Durable;

        private DurabilityVerdict( DurabilityKind kind, ImmutableArray<string> chain, string? reason )
        {
            this.Kind = kind;
            this.Chain = chain;
            this.Reason = reason;
        }

        /// <summary>
        /// Creates the verdict of a type that is not durable, starting a new chain at that type.
        /// </summary>
        public static DurabilityVerdict NotDurable( string typeName, string? reason )
            => new( DurabilityKind.NotDurable, ImmutableArray.Create( typeName ), reason );

        /// <summary>
        /// Creates the verdict of an interface or abstract type that does not carry the attribute, starting a new
        /// chain at that type.
        /// </summary>
        public static DurabilityVerdict NotAnnotated( string typeName, string? reason )
            => new( DurabilityKind.NotAnnotated, ImmutableArray.Create( typeName ), reason );

        /// <summary>
        /// Returns a verdict identical to the current one except that the given step is prepended to its chain.
        /// </summary>
        /// <remarks>
        /// The chain is built as the recursion unwinds, so the cost is proportional to the length of the failing path
        /// rather than to the size of the graph that was searched.
        /// </remarks>
        public DurabilityVerdict Prepend( string step )
            => this.IsDurable ? this : new DurabilityVerdict( this.Kind, this.Chain.Insert( 0, step ), this.Reason );

        /// <summary>
        /// Formats the chain for a diagnostic message.
        /// </summary>
        /// <remarks>
        /// The format matches the one that a memory-leak test failure and a <c>UserCodeRetentionAnalyzer</c> finding
        /// print, so that a static and a runtime report of the same defect read alike.
        /// </remarks>
        public string FormatChain()
        {
            const int maxSteps = 8;

            var steps = this.Chain.Length <= maxSteps
                ? string.Join( " -> ", this.Chain )
                : string.Join( " -> ", this.Chain, 0, maxSteps ) + " -> ...";

            return this.Reason == null ? steps : steps + " (" + this.Reason + ")";
        }
    }
}
