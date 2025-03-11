// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace PostSharp.Aspects
{
    [PublicAPI]
    public class TypeIdentity
    {
        public static TypeIdentity FromType( Type type )
        {
            throw new NotImplementedException();
        }

        public static TypeIdentity[] FromTypes( Type[] types )
        {
            throw new NotImplementedException();
        }

        public Type Type { get; }

        public Type ToType()
        {
            throw new NotImplementedException();
        }

        public static TypeIdentity FromTypeName( string typeName )
        {
            throw new NotImplementedException();
        }

        public static TypeIdentity[] FromTypeNames( string[] typeNames )
        {
            throw new NotImplementedException();
        }

        public string TypeName { get; }
    }
}