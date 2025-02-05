// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Introspection;

public enum IntrospectionChildKinds
{
    None,
    DerivedType = 1,
    ContainingDeclaration = 2,
    All = DerivedType | ContainingDeclaration
}