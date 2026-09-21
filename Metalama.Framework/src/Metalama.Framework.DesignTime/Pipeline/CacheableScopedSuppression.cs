// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.DesignTime.Pipeline;

/// <summary>
/// A compilation-independent version of <see cref="ScopedSuppression"/>, which stores the symbol id instead of the <see cref="ISymbol"/> itself.
/// </summary>
internal sealed class CacheableScopedSuppression : IScopedSuppression
{
    public ISuppression Suppression { get; }

    ISymbol? IScopedSuppression.GetScopeSymbolOrNull( CompilationContext compilationContext ) => this.DeclarationId.ResolveToSymbolOrNull( compilationContext );

    public SerializableDeclarationId DeclarationId { get; }

    private CacheableScopedSuppression( ScopedSuppression suppression, SerializableDeclarationId declarationId )
    {
        this.Suppression = suppression.Suppression;
        this.DeclarationId = declarationId;
    }

    /// <summary>
    /// Creates a <see cref="CacheableScopedSuppression"/> from a <see cref="ScopedSuppression"/>, unless the declaration
    /// the suppression applies to has no <see cref="SerializableDeclarationId"/>.
    /// </summary>
    /// <remarks>
    /// A declaration of a file-local type does have an identifier, because the identifier carries the metadata name of
    /// that type as a discriminator. This method still reports a failure, because a local function, a local variable and
    /// a module have no identifier, and so does a reference to an attribute. Such a scope symbol is skipped rather than
    /// aborting the whole pass, which is what lost the result of the entire project. See issue #2051.
    /// </remarks>
    public static bool TryCreate( ScopedSuppression suppression, [NotNullWhen( true )] out CacheableScopedSuppression? cacheableSuppression )
    {
        if ( !suppression.ScopeSymbol.TryGetSerializableId( out var declarationId ) )
        {
            cacheableSuppression = null;

            return false;
        }

        cacheableSuppression = new CacheableScopedSuppression( suppression, declarationId );

        return true;
    }

    public override string ToString() => $"{this.Suppression} on {this.DeclarationId}";
}
