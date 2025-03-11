// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Eligibility.Implementation;

namespace Metalama.Framework.Eligibility;

public static partial class EligibilityExtensions
{
    /// <summary>
    /// A helper type that allows to convert an <see cref="IEligibilityBuilder{T}"/> for a type to an <see cref="IEligibilityBuilder{T}"/> of another type.  
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct Converter<T>
        where T : class
    {
        private readonly IEligibilityBuilder<T> _eligibilityBuilder;

        internal Converter( IEligibilityBuilder<T> eligibilityBuilder )
        {
            this._eligibilityBuilder = eligibilityBuilder;
        }

        /// <summary>
        /// Gets an <see cref="IEligibilityBuilder{T}"/> for another type. Adds an eligibility rule that the validated object must be of the specified type.
        /// If the validated object is not of the specified type, the parent eligibility rule fails.
        /// </summary>
        /// <seealso cref="When{TOutput}"/>
        public IEligibilityBuilder<TOutput> To<TOutput>()
            where TOutput : class, T
            => new ChildEligibilityBuilder<T, TOutput>(
                this._eligibilityBuilder,
                d => (TOutput) d,
                d => d.Description!,
                d => d is TOutput,
                d => $"{d} must be a {GetInterfaceName<TOutput>()}" );

        /// <summary>
        /// Gets an <see cref="IEligibilityBuilder"/> for another type, but only adds the rule when the validated object is of the given type.
        /// If the validated object is not of the specified type, the child eligibility rule is ignored. Uses <see cref="EligibilityExtensions.If{T}"/>.
        /// </summary>
        public IEligibilityBuilder<TOutput> When<TOutput>()
            where TOutput : class, T
            => this._eligibilityBuilder.If( d => d is TOutput ).Convert().To<TOutput>();
    }
}