// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Eligibility.Implementation;

namespace Metalama.Framework.Eligibility;

public static partial class EligibilityExtensions
{
    /// <summary>
    /// A helper type that allows converting an <see cref="IEligibilityBuilder{T}"/> to an <see cref="IEligibilityBuilder{T}"/>
    /// for a more specific type, with different conversion semantics.
    /// </summary>
    /// <typeparam name="T">The source declaration type.</typeparam>
    /// <remarks>
    /// <para>
    /// This struct is returned by <see cref="EligibilityExtensions.Convert{T}"/> and provides two conversion methods:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="To{TOutput}"/> - Adds an eligibility requirement that the declaration must be of the target type.</description></item>
    /// <item><description><see cref="When{TOutput}"/> - Conditionally applies rules only when the declaration is of the target type, without adding a requirement.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="EligibilityExtensions.Convert{T}"/>
    public readonly struct Converter<T>
        where T : class
    {
        private readonly IEligibilityBuilder<T> _eligibilityBuilder;

        internal Converter( IEligibilityBuilder<T> eligibilityBuilder )
        {
            this._eligibilityBuilder = eligibilityBuilder;
        }

        /// <summary>
        /// Converts to an <see cref="IEligibilityBuilder{T}"/> for a more specific type and adds an eligibility requirement
        /// that the declaration must be of the specified type.
        /// </summary>
        /// <typeparam name="TOutput">The more specific type to convert to (must be derived from or the same as <typeparamref name="T"/>).</typeparam>
        /// <returns>An eligibility builder for <typeparamref name="TOutput"/>.</returns>
        /// <remarks>
        /// <para>
        /// If the validated object is not of type <typeparamref name="TOutput"/>, the declaration becomes ineligible.
        /// Use this when you want to ensure the declaration is of a specific type and add additional rules for that type.
        /// </para>
        /// <para>
        /// For conditional rules that only apply when the declaration happens to be of a certain type (without making it ineligible
        /// if it's not), use <see cref="When{TOutput}"/> instead.
        /// </para>
        /// </remarks>
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
        /// Converts to an <see cref="IEligibilityBuilder{T}"/> for a more specific type, but only applies rules conditionally
        /// when the declaration is of the specified type. Does not add an eligibility requirement.
        /// </summary>
        /// <typeparam name="TOutput">The more specific type to convert to (must be derived from or the same as <typeparamref name="T"/>).</typeparam>
        /// <returns>An eligibility builder for <typeparamref name="TOutput"/> where rules only apply when the declaration is of that type.</returns>
        /// <remarks>
        /// <para>
        /// If the validated object is not of type <typeparamref name="TOutput"/>, the rules added to the returned builder
        /// are simply ignored (the declaration is not made ineligible). This is equivalent to using <see cref="EligibilityExtensions.If{T}"/>
        /// followed by <see cref="To{TOutput}"/>.
        /// </para>
        /// <para>
        /// Use this when you want to add type-specific rules without requiring that all declarations be of that type.
        /// For example, you might want to add special rules for methods without making fields ineligible.
        /// </para>
        /// </remarks>
        /// <seealso cref="To{TOutput}"/>
        public IEligibilityBuilder<TOutput> When<TOutput>()
            where TOutput : class, T
            => this._eligibilityBuilder.If( d => d is TOutput ).Convert().To<TOutput>();
    }
}