// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Code.Collections
{
    /// <summary>
    /// Extension methods for the <see cref="IParameterList"/> class.
    /// </summary>
    [CompileTime]
    public static class ParameterListExtensions
    {
        /// <summary>
        /// Selects the parameters of a given type.
        /// </summary>
        public static IEnumerable<IParameter> OfParameterType<T>( this IParameterList parameters ) => parameters.OfParameterType( typeof(T) );

        /// <summary>
        /// Selects the parameters of a given type.
        /// </summary>
        public static IEnumerable<IParameter> OfParameterType( this IParameterList parameters, Type type )
            => parameters.Where( p => p.Type.IsConvertibleTo( type ) );

        /// <summary>
        /// Selects the parameters of a given type.
        /// </summary>
        public static IEnumerable<IParameter> OfParameterType( this IParameterList parameters, IType type )
            => parameters.Where( p => p.Type.IsConvertibleTo( type ) );

        public static IParameter? OfName( this IParameterList list, string name ) => list.SingleOrDefault( p => p.Name == name );
    }
}