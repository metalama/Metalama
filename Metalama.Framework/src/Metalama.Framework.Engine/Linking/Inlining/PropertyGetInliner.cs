// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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