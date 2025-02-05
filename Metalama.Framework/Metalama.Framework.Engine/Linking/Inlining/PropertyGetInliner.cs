// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Linking.Inlining;

internal abstract class PropertyGetInliner : PropertyInliner
{
    public override bool IsValidForTargetSymbol( ISymbol symbol )
    {
        var property =
            symbol as IPropertySymbol ?? (symbol is IMethodSymbol { AssociatedSymbol: IPropertySymbol associatedProperty }
                ? associatedProperty
                : null);

        return property is { GetMethod: not null }
               && !property.GetMethod.IsIteratorMethod();
    }
}