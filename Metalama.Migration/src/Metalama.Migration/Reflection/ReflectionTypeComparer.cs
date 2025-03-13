// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;
using System.Collections.Generic;

namespace PostSharp.Reflection
{
    /// <summary>
    /// In Metalama, use <see cref="ICompilation"/>.<see cref="ICompilation.Comparers"/>.
    /// </summary>
    public sealed class ReflectionTypeComparer : IEqualityComparer<Type>, IEqualityComparer<Type[]>
    {
        public static ReflectionTypeComparer GetInstance()
        {
            throw new NotImplementedException();
        }

        public static ReflectionTypeComparer GetInstance(
            Type[] leftGenericTypeParameters,
            Type[] leftGenericMethodParameters,
            Type[] rightGenericTypeParameters,
            Type[] rightGenericMethodParameters )
        {
            throw new NotImplementedException();
        }

        public bool Equals( Type x, Type y )
        {
            throw new NotImplementedException();
        }

        public int GetHashCode( Type obj )
        {
            throw new NotImplementedException();
        }

        #region IEqualityComparer<Type[]> Members

        public bool Equals( Type[] x, Type[] y )
        {
            throw new NotImplementedException();
        }

        public int GetHashCode( Type[] types )
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}