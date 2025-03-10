// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Metalama.Framework.Engine.Linking;

internal sealed class InliningContextIdentifierEqualityComparer : IEqualityComparer<InliningContextIdentifier?>
{
    private readonly IEqualityComparer<ISymbol> _symbolComparer;

    private InliningContextIdentifierEqualityComparer( IEqualityComparer<ISymbol> symbolComparer )
    {
        this._symbolComparer = symbolComparer;
    }

    public static IEqualityComparer<InliningContextIdentifier> ForCompilation( CompilationContext context )
        => new InliningContextIdentifierEqualityComparer( context.SymbolComparer );

    public bool Equals( InliningContextIdentifier? x, InliningContextIdentifier? y )
        => (x == null && y == null)
           || (
               x != null && y != null &&
               x.InliningId?.Value == y.InliningId?.Value
               && this._symbolComparer.Equals( x.DestinationSemantic.Symbol, y.DestinationSemantic.Symbol )
               && x.DestinationSemantic.Kind == y.DestinationSemantic.Kind);

    public int GetHashCode( InliningContextIdentifier? x )
    {
        if ( x == null )
        {
            return 0;
        }
        else
        {
            // PERF: Cast enum to int otherwise it will be boxed on .NET Framework.
            return HashCode.Combine(
                x.InliningId?.Value,
                this._symbolComparer.GetHashCode( x.DestinationSemantic.Symbol ),
                (int) x.DestinationSemantic.Kind );
        }
    }
}