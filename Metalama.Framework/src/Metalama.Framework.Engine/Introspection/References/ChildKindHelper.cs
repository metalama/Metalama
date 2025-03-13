// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Validation;
using Metalama.Framework.Introspection;
using System;

namespace Metalama.Framework.Engine.Introspection.References;

internal static class ChildKindHelper
{
    public static ChildKinds ToChildKinds( IntrospectionChildKinds kind )
        => kind switch
        {
            IntrospectionChildKinds.All => ChildKinds.All,
            IntrospectionChildKinds.None => ChildKinds.None,
            IntrospectionChildKinds.ContainingDeclaration => ChildKinds.ContainingDeclaration,
            IntrospectionChildKinds.DerivedType => ChildKinds.DerivedType,
            _ => throw new ArgumentOutOfRangeException()
        };
}