// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Utilities;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.DesignTime.Pipeline;

/// <summary>
/// A compilation-independent version of <see cref="ScopedSuppression"/>, which stores the symbol id instead of the <see cref="ISymbol"/> itself.
/// </summary>
[Durable]
internal sealed class CacheableScopedSuppression : IScopedSuppression
{
    /// <remarks>
    /// <see cref="ISuppression.Filter"/> is a delegate, and <c>SuppressionDefinition.WithFilter</c> produces an
    /// implementation that captures the user's lambda, so this is the one member of the durable design-time surface
    /// typed as an interface that carries a concrete risk rather than a hypothetical one. It is worth measuring
    /// before the general question is settled. <c>SuppressionDefinition</c> itself returns <c>null</c> for the filter
    /// and is fine.
    /// </remarks>
    [SuppressMessage(
        "Metalama",
        "LAMA0876:An interface or abstract type used by a durable type is not marked [Durable]",
        Justification =
            "See \"Should the contract propagate to the user-implementable interfaces?\" in design-time-memory.md, "
            + "which records this member as the one to measure first." )]
    public ISuppression Suppression { get; }

    ISymbol? IScopedSuppression.GetScopeSymbolOrNull( CompilationContext compilationContext ) => this.DeclarationId.ResolveToSymbolOrNull( compilationContext );

    public SerializableDeclarationId DeclarationId { get; }

    public CacheableScopedSuppression( ScopedSuppression suppression )
    {
        this.Suppression = suppression.Suppression;
        this.DeclarationId = suppression.ScopeSymbol.GetSerializableId();
    }

    public override string ToString() => $"{this.Suppression} on {this.DeclarationId}";
}