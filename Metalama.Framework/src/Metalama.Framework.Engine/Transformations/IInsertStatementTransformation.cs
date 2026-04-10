// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Transformations;

/// <summary>
/// Represents a code transformation that inserts statements into the target member or extension block.
/// </summary>
internal interface IInsertStatementTransformation : ISyntaxTreeTransformation
{
    /// <summary>
    /// Provides a list of inserted statements. When this transformation implements
    /// <see cref="IAggregatableInsertStatementTransformation"/> and the linker has collected adjacent peers with the
    /// same <see cref="IAggregatableInsertStatementTransformation.AggregateKey"/>, only the first transformation in the
    /// group receives this call, and <paramref name="aggregatedGroup"/> contains the full ordered group including
    /// <c>this</c>. Otherwise <paramref name="aggregatedGroup"/> is <c>null</c>.
    /// </summary>
    /// <param name="context">Context for providing inserted statements.</param>
    /// <param name="aggregatedGroup">
    /// When non-null, the ordered list of aggregated peer transformations (including <c>this</c> as the first element);
    /// <c>null</c> when this transformation is being invoked independently.
    /// </param>
    /// <returns>A list of inserted statements, or an empty list if an error occurred.</returns>
    IReadOnlyList<InsertedStatement> GetInsertedStatements(
        InsertStatementTransformationContext context,
        IReadOnlyList<IInsertStatementTransformation>? aggregatedGroup = null );

    /// <summary>
    /// Gets the member or extension block that statements will be inserted into (may differ from <see cref="ITransformationBase.TargetDeclaration"/> e.g. for builders).
    /// For extension blocks, statements are inserted into all instance members.
    /// </summary>
    IFullRef<IMemberOrNamedType> TargetMemberOrNamedType { get; }
}

/// <summary>
/// Optional interface that an <see cref="IInsertStatementTransformation"/> can implement to opt in to aggregation.
/// The linker groups transformations sharing the same <see cref="AggregateKey"/> and the same
/// <see cref="IInsertStatementTransformation.TargetMemberOrNamedType"/> (adjacency is evaluated after filtering by
/// that <c>(target, key)</c> pair, so non-matching transformations interleaved by other aspects don't break the
/// group), collects them into an ordered list preserving the original linker order, and calls
/// <see cref="IInsertStatementTransformation.GetInsertedStatements"/> only on the first one, passing the full
/// ordered group as the <c>aggregatedGroup</c> argument. This enables coordinated statement production across
/// multiple independent advices — for example, merging slot expressions from several advices into a single
/// <c>base.OnConstructed(context.Descend(slotA | slotB))</c> call, or deduplicating identical epilogues produced
/// by peer advices.
/// </summary>
internal interface IAggregatableInsertStatementTransformation : IInsertStatementTransformation
{
    /// <summary>
    /// Gets the key used by the linker to aggregate transformations sharing the same target. Must be non-null.
    /// Keys are compared with <see cref="object.Equals(object?)"/>, so implementers may use a string literal,
    /// a static singleton <c>object</c>, or any other instance with suitable equality semantics.
    /// </summary>
    object AggregateKey { get; }
}