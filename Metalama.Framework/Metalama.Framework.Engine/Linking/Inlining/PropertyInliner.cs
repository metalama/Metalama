// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Linking.Inlining;

internal abstract class PropertyInliner : Inliner
{
    public override bool IsValidForTargetSymbol( ISymbol symbol ) => symbol is IPropertySymbol or IMethodSymbol { AssociatedSymbol: IPropertySymbol };

    public override bool IsValidForContainingSymbol( ISymbol symbol ) => true;
}