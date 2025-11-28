// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Project;
using System;

namespace Metalama.Framework.Eligibility
{
    /// <summary>
    /// A concrete implementation of <see cref="IDescribedObject{T}"/> that encapsulates an object and its human-readable description.
    /// </summary>
    /// <typeparam name="T">The type of object being described.</typeparam>
    /// <remarks>
    /// This class is used internally by the eligibility system to provide formatted error messages when eligibility rules fail.
    /// User code rarely needs to create instances of this class directly.
    /// </remarks>
    /// <seealso cref="IDescribedObject{T}"/>
    /// <seealso href="@eligibility"/>
    public sealed class DescribedObject<T> : IDescribedObject<T>
    {
        /// <inheritdoc />
        public T Object { get; }

        /// <inheritdoc />
        public FormattableString? Description { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DescribedObject{T}"/> class.
        /// </summary>
        /// <param name="o">The object to describe.</param>
        /// <param name="description">An optional human-readable description of the object. If <c>null</c>, a default description will be generated.</param>
        public DescribedObject( T o, FormattableString? description = null )
        {
            this.Object = o;
            this.Description = description;
        }

        string IFormattable.ToString( string? format, IFormatProvider? formatProvider ) // ReSharper disable FormatStringProblem
        {
            var theFormatProvider = formatProvider ?? MetalamaExecutionContext.Current.FormatProvider;

            return this.Description?.ToString( theFormatProvider )
                   ?? string.Format( theFormatProvider, "'{0:" + format + "}'", this.Object );
        }
    }
}