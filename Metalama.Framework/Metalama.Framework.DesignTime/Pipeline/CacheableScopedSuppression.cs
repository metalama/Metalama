// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.Pipeline;

/// <summary>
/// A compilation-independent version of <see cref="ScopedSuppression"/>, which stores the symbol id instead of the <see cref="ISymbol"/> itself.
/// </summary>
internal sealed class CacheableScopedSuppression : IScopedSuppression
{
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