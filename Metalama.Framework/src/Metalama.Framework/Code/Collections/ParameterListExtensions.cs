// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
        /// <typeparam name="T">The type to filter parameters by, which must be a compile-time or run-time-or-compile-time type.</typeparam>
        /// <param name="parameters">The parameter list to filter.</param>
        /// <returns>An enumerable of parameters whose types are convertible to <typeparamref name="T"/>.</returns>
        public static IEnumerable<IParameter> OfParameterType<T>( this IParameterList parameters ) => parameters.OfParameterType( typeof(T) );

        /// <summary>
        /// Selects the parameters of a given type.
        /// </summary>
        /// <param name="parameters">The parameter list to filter.</param>
        /// <param name="type">The reflection type to filter parameters by.</param>
        /// <returns>An enumerable of parameters whose types are convertible to <paramref name="type"/>.</returns>
        public static IEnumerable<IParameter> OfParameterType( this IParameterList parameters, Type type )
            => parameters.Where( p => p.Type.IsConvertibleTo( type ) );

        /// <summary>
        /// Selects the parameters of a given type.
        /// </summary>
        /// <param name="parameters">The parameter list to filter.</param>
        /// <param name="type">The Metalama type to filter parameters by.</param>
        /// <returns>An enumerable of parameters whose types are convertible to <paramref name="type"/>.</returns>
        public static IEnumerable<IParameter> OfParameterType( this IParameterList parameters, IType type )
            => parameters.Where( p => p.Type.IsConvertibleTo( type ) );

        /// <summary>
        /// Gets the parameter with the specified name.
        /// </summary>
        /// <param name="list">The parameter list to search.</param>
        /// <param name="name">The name of the parameter to find.</param>
        /// <returns>The parameter with the specified name, or <c>null</c> if no such parameter exists.</returns>
        public static IParameter? OfName( this IParameterList list, string name ) => list.SingleOrDefault( p => p.Name == name );
    }
}