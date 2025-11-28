// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;
using System;

namespace Metalama.Framework.Eligibility
{
    /// <summary>
    /// Encapsulates an arbitrary object (typically a declaration) along with its optional human-readable description,
    /// used when generating eligibility error messages.
    /// </summary>
    /// <typeparam name="T">The type of object being described (typically a declaration type like <see cref="Code.IMethod"/> or <see cref="Code.IType"/>).</typeparam>
    /// <remarks>
    /// <para>
    /// This interface is primarily used in eligibility justification methods, particularly in
    /// <see cref="EligibilityExtensions.MustSatisfy{T}"/> where the <c>getJustification</c> parameter receives an
    /// <see cref="IDescribedObject{T}"/> to generate error messages.
    /// </para>
    /// <para>
    /// The <see cref="Description"/> property provides a formatted description of the object that can be embedded
    /// in error messages. When used in a <see cref="FormattableString"/>, the object will be properly formatted
    /// by the framework's custom formatter.
    /// </para>
    /// <para>
    /// User code typically does not implement this interface directly. It is provided by the framework when
    /// eligibility rules are evaluated.
    /// </para>
    /// </remarks>
    /// <seealso cref="IEligibilityRule{T}.GetIneligibilityJustification"/>
    /// <seealso cref="EligibilityExtensions.MustSatisfy{T}"/>
    /// <seealso href="@eligibility"/>
    [InternalImplement]
    [CompileTime]
    public interface IDescribedObject<out T> : IFormattable
    {
        /// <summary>
        /// Gets the object being described (typically a declaration such as a method, type, or parameter).
        /// </summary>
        /// <remarks>
        /// Access this property to inspect the actual declaration when writing custom eligibility predicates
        /// in <see cref="EligibilityExtensions.MustSatisfy{T}"/>.
        /// </remarks>
        T Object { get; }

        /// <summary>
        /// Gets an optional human-readable description of <see cref="Object"/>, suitable for inclusion in error messages.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property returns a <see cref="FormattableString"/> that can be embedded directly in eligibility
        /// error messages. The framework's custom formatter ensures proper rendering of declarations.
        /// </para>
        /// <para>
        /// When writing justification messages in <see cref="EligibilityExtensions.MustSatisfy{T}"/>, you can
        /// use the <see cref="IDescribedObject{T}"/> parameter directly in interpolated strings, and it will
        /// be formatted appropriately.
        /// </para>
        /// </remarks>
        FormattableString? Description { get; }
    }
}