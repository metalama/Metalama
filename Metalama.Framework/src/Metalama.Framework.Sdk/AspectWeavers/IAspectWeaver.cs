// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.AspectWeavers
{
    /// <summary>
    /// Defines an aspect weaver that applies low-level transformations to Roslyn compilations. Aspect weavers
    /// are used for low-level transformations only and don't fully integrate with high-level aspects. Implementations
    /// must be public, have a default constructor, and be annotated with the <see cref="MetalamaPlugInAttribute"/> custom attribute.
    /// </summary>
    /// <remarks>
    /// Aspect weavers provide a way to perform custom Roslyn syntax transformations that are not possible with
    /// standard Metalama aspects. They operate at a lower level than high-level aspects and have direct access
    /// to the Roslyn compilation.
    /// </remarks>
    /// <seealso cref="AspectWeaverContext"/>
    /// <seealso cref="MetalamaPlugInAttribute"/>
    /// <seealso cref="IAspectDriver"/>
    [CompileTime]
    public interface IAspectWeaver : IAspectDriver
    {
        /// <summary>
        /// Transforms a Roslyn compilation according to the aspects being woven.
        /// </summary>
        /// <param name="context">The context providing access to the compilation and weaving services.</param>
        /// <returns>A task representing the asynchronous transformation operation.</returns>
        Task TransformAsync( AspectWeaverContext context );
    }
}