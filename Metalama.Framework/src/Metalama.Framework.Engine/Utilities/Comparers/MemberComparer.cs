// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Utilities.Comparers
{
    internal sealed class MemberComparer<T> : IEqualityComparer<T>
        where T : class, IMember
    {
        public MemberComparer( IEqualityComparer<IType> typeComparer )
        {
            this._typeComparer = typeComparer;
        }

        private readonly IEqualityComparer<IType> _typeComparer;

        public bool Equals( T? x, T? y )
        {
            if ( ReferenceEquals( x, y ) )
            {
                return true;
            }
            else if ( x == null || y == null )
            {
                return false;
            }

            if ( x.Name != y.Name )
            {
                return false;
            }

            if ( x.IsStatic != y.IsStatic )
            {
                return false;
            }

            if ( x.DeclarationKind != y.DeclarationKind )
            {
                return false;
            }

            if ( x is IHasParameters xHasParameters )
            {
                var yHasParameters = (IHasParameters) y;

                if ( xHasParameters.Parameters.Count != yHasParameters.Parameters.Count )
                {
                    return false;
                }

                for ( var i = 0; i < xHasParameters.Parameters.Count; i++ )
                {
                    var xParameter = xHasParameters.Parameters[i].Type;
                    var yParameter = yHasParameters.Parameters[i].Type;

                    if ( !this._typeComparer.Equals( xParameter, yParameter ) )
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public int GetHashCode( T obj )
        {
            var hashCode = default(HashCode);
            hashCode.Add( obj.Name );

            if ( obj is IHasParameters hasParameters )
            {
                foreach ( var parameter in hasParameters.Parameters )
                {
                    hashCode.Add( parameter.Type, this._typeComparer );
                }
            }

            return hashCode.ToHashCode();
        }
    }
}