// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Linking;

internal sealed class AspectReferenceTargetEqualityComparer : IEqualityComparer<AspectReferenceTarget>
{
    private readonly IEqualityComparer<ISymbol> _symbolComparer;

    private AspectReferenceTargetEqualityComparer( IEqualityComparer<ISymbol> symbolComparer )
    {
        this._symbolComparer = symbolComparer;
    }

    public static IEqualityComparer<AspectReferenceTarget> ForCompilation( CompilationContext context )
        => new AspectReferenceTargetEqualityComparer( context.SymbolComparer );

    public bool Equals( AspectReferenceTarget x, AspectReferenceTarget y )
        => this._symbolComparer.Equals( x.Symbol, y.Symbol )
           && x.SemanticKind == y.SemanticKind
           && x.TargetKind == y.TargetKind;

    public int GetHashCode( AspectReferenceTarget x )
        =>

            // PERF: Cast enum to byte otherwise it will be boxed on .NET Framework.
            HashCode.Combine(
                this._symbolComparer.GetHashCode( x.Symbol ),
                (byte) x.SemanticKind,
                (byte) x.TargetKind );
}