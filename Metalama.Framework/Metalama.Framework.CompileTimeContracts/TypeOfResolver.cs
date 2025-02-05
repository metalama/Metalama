// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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