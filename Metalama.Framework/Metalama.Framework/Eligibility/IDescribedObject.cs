// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;
using System;

namespace Metalama.Framework.Eligibility
{
    /// <summary>
    /// Encapsulates an arbitrary object and its optional human-readable description.
    /// Implemented by <see cref="DescribedObject{T}"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso href="@eligibility"/>
    [InternalImplement]
    [CompileTime]
    public interface IDescribedObject<out T> : IFormattable
    {
        /// <summary>
        /// Gets the described object.
        /// </summary>
        T Object { get; }

        /// <summary>
        /// Gets the optional human-readable description of <see cref="Object"/>.
        /// </summary>
        FormattableString? Description { get; }
    }
}