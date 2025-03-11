// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.AspectWeavers
{
    /// <summary>
    /// Aspect weavers are responsible for applying low-level aspects to the Roslyn transformation. They
    /// are used for low-level transformations only, and don't totally integrate with high-level aspects. Implementations
    /// of this class must be public, have a default constructor, and be annotated with the <see cref="MetalamaPlugInAttribute"/> custom attribute.
    /// </summary>
    [CompileTime]
    public interface IAspectWeaver : IAspectDriver
    {
        /// <summary>
        /// Transforms a Roslyn compilation according to some given aspects.
        /// </summary>
        /// <param name="context"></param>
        Task TransformAsync( AspectWeaverContext context );
    }
}