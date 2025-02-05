// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Linking.Inlining;

internal abstract class EventInliner : Inliner
{
    public override bool IsValidForTargetSymbol( ISymbol symbol ) => symbol is IEventSymbol or IMethodSymbol { AssociatedSymbol: IEventSymbol };

    public override bool IsValidForContainingSymbol( ISymbol symbol ) => true;
}