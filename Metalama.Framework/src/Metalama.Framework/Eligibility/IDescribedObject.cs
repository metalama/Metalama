// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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