// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.CompileTimeContracts;

[PublicAPI]
public static class TypeOfResolver
{
    public static Type Resolve( string typeId, IReadOnlyDictionary<string, IType>? substitutions = null )
    {
        if ( TypeIdResolver == null )
        {
            throw new InvalidOperationException( "The service is not properly initialized." );
        }

        return TypeIdResolver( typeId, substitutions );
    }

    public static Type Resolve( string typeId, string? ns, string name, string fullName, string toString )
    {
        if ( DeclarationIdResolver == null )
        {
            throw new InvalidOperationException( "The service is not properly initialized." );
        }

        return DeclarationIdResolver( typeId, ns, name, fullName, toString );
    }

    internal static Func<string, IReadOnlyDictionary<string, IType>?, Type>? TypeIdResolver { get; set; }

    internal static Func<string, string?, string, string, string, Type>? DeclarationIdResolver { get; set; }
}