// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Defines a method <see cref="ToDisplayString"/> that renders the current declaration into a human-readable
    /// string.
    /// </summary>
    [CompileTime]
    [InternalImplement]
    public interface IDisplayable
    {
        /// <summary>
        /// Renders the current declaration into a human-readable string.
        /// </summary>
        /// <param name="format">Reserved for future use. Specifies formatting options.</param>
        /// <param name="context">Reserved for future use. Specifies the context in which the string must be displayed. This allow to abbreviate a few pieces of information.</param>
        /// <returns>A human-readable string for the current declaration.</returns>
        string ToDisplayString( CodeDisplayFormat? format = null, CodeDisplayContext? context = null );
    }
}