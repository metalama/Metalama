// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Exposes as a dictionary the tags passed to an advise.
    /// </summary>
    [CompileTime]
    public interface IObjectReader : IReadOnlyDictionary<string, object?>
    {
        /// <summary>
        /// Gets the source object (typically an object of an anonymous type).
        /// </summary>
        /// <remarks>
        ///  If there are several source objects (i.e. when both <c>aspectBuilder.Tags</c> and the
        /// <c>tags</c> method parameter are set), this propertyr returns an <c>ImmutableArray&lt;object&gt;</c>
        /// with all sources.
        /// </remarks>
        object? Source { get; }
    }
}