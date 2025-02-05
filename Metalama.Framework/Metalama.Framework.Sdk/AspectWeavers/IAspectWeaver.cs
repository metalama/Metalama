// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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