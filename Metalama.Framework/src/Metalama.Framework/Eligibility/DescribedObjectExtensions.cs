// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Eligibility
{
    /// <summary>
    /// Extension methods for <see cref="IDescribedObject{T}"/>.
    /// </summary>
    /// <seealso href="@eligibility"/>
    [CompileTime]
    public static class DescribedObjectExtensions
    {
        /// <summary>
        /// Casts the described object to a different type.
        /// </summary>
        /// <typeparam name="TIn">The input type of the described object.</typeparam>
        /// <typeparam name="TOut">The output type to cast to.</typeparam>
        /// <param name="describedObject">The described object to cast.</param>
        /// <returns>A new described object with the object cast to <typeparamref name="TOut"/> and the same description.</returns>
        public static IDescribedObject<TOut> Cast<TIn, TOut>( this IDescribedObject<TIn> describedObject )
            => new DescribedObject<TOut>( (TOut) (object) describedObject.Object!, describedObject.Description );
    }
}