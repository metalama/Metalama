// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Project;
using System;

namespace Metalama.Framework.Eligibility
{
    /// <summary>
    /// Encapsulates an object and a human-readable description.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso href="@eligibility"/>
    public sealed class DescribedObject<T> : IDescribedObject<T>
    {
        /// <inheritdoc />
        public T Object { get; }

        /// <inheritdoc />
        public FormattableString? Description { get; }

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