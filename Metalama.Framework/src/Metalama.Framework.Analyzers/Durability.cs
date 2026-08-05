// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;

namespace Metalama.Framework.Analyzers
{
    /// <summary>
    /// The verdict of the durability evaluator for a type.
    /// </summary>
    internal enum DurabilityKind
    {
        /// <summary>
        /// The type is safe to be held across compilations.
        /// </summary>
        Durable,

        /// <summary>
        /// The type reaches, or may reach, an object that is bound to a compilation.
        /// </summary>
        NotDurable,

        /// <summary>
        /// The type is an interface or an abstract type that is not marked. Reported separately from
        /// <see cref="NotDurable"/> because the remedy differs in kind: marking a class is verified against its own
        /// members, whereas marking an interface exports the obligation to every implementation, which the analyzer
        /// then verifies in turn. It is not undecidable, but the guarantee reaches only the implementations that are
        /// compiled with this analyzer, so a project may reasonably weigh it differently.
        /// </summary>
        UnmarkedInterface
    }

    /// <summary>
    /// How a well-known type is classified, without examining its members.
    /// </summary>
    internal enum WellKnownDurability
    {
        /// <summary>
        /// Durable whatever its type arguments are. Also used for the types at which the walk stops because a chain
        /// through them explains nothing, which mirrors <c>UserCodeRetentionPolicy.IsBoundary</c>.
        /// </summary>
        Durable,

        /// <summary>
        /// Never durable.
        /// </summary>
        NotDurable,

        /// <summary>
        /// Durable exactly when the type arguments selected by the argument mask are durable.
        /// </summary>
        Transparent
    }

    /// <summary>
    /// An entry of one of the well-known type tables.
    /// </summary>
    internal readonly struct WellKnownEntry
    {
        /// <summary>
        /// Gets the classification of the type.
        /// </summary>
        public WellKnownDurability Durability { get; }

        /// <summary>
        /// Gets the explanation that appears at the end of the retention chain in the diagnostic message, or
        /// <c>null</c> when the classification needs none.
        /// </summary>
        public string? Reason { get; }

        /// <summary>
        /// Gets the indices of the type arguments that must be durable, or <c>null</c> when all of them must be.
        /// Relevant only when <see cref="Durability"/> is <see cref="WellKnownDurability.Transparent"/>.
        /// </summary>
        /// <remarks>
        /// The mask exists for <c>ConditionalWeakTable{TKey,TValue}</c>, whose key is not kept alive by the table and
        /// must therefore be ignored.
        /// </remarks>
        public ImmutableArray<int> ArgumentMask { get; }

        public WellKnownEntry( WellKnownDurability durability, string? reason = null, ImmutableArray<int> argumentMask = default )
        {
            this.Durability = durability;
            this.Reason = reason;
            this.ArgumentMask = argumentMask;
        }
    }

    /// <summary>
    /// The result of evaluating the durability of a type, with the chain of members and types that explains a
    /// negative verdict.
    /// </summary>
    /// <remarks>
    /// The chain holds strings and never symbols. A diagnostic is retained by Roslyn for as long as the compilation
    /// that produced it, so an analyzer that reported a symbol as a message argument would keep that symbol alive.
    /// That is the discipline this analyzer exists to enforce, and it applies to the analyzer itself.
    /// </remarks>
    internal sealed class Verdict
    {
        /// <summary>
        /// The verdict of a type that is durable. A singleton, because it is by far the most frequent result and it
        /// carries no chain.
        /// </summary>
        public static readonly Verdict Durable = new( DurabilityKind.Durable, ImmutableArray<string>.Empty, null );

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

        private Verdict( DurabilityKind kind, ImmutableArray<string> chain, string? reason )
        {
            this.Kind = kind;
            this.Chain = chain;
            this.Reason = reason;
        }

        /// <summary>
        /// Creates the verdict of a type that is not durable, starting a new chain at that type.
        /// </summary>
        public static Verdict NotDurable( string typeName, string? reason )
            => new( DurabilityKind.NotDurable, ImmutableArray.Create( typeName ), reason );

        /// <summary>
        /// Creates the verdict of an interface or abstract type that is not marked, starting a new chain at that type.
        /// </summary>
        public static Verdict UnmarkedInterface( string typeName, string? reason )
            => new( DurabilityKind.UnmarkedInterface, ImmutableArray.Create( typeName ), reason );

        /// <summary>
        /// Returns a verdict identical to the current one except that the given step is prepended to its chain.
        /// </summary>
        /// <remarks>
        /// The chain is built as the recursion unwinds, so the cost is proportional to the length of the failing path
        /// rather than to the size of the graph that was searched.
        /// </remarks>
        public Verdict Prepend( string step )
            => this.IsDurable ? this : new Verdict( this.Kind, this.Chain.Insert( 0, step ), this.Reason );

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
