// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
        public static IDescribedObject<TOut> Cast<TIn, TOut>( this IDescribedObject<TIn> describedObject )
            => new DescribedObject<TOut>( (TOut) (object) describedObject.Object!, describedObject.Description );
    }
}