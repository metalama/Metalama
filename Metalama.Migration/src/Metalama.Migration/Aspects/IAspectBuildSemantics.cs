// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using PostSharp.Aspects.Configuration;
using PostSharp.Extensibility;

namespace PostSharp.Aspects
{
    /// <summary>
    /// Aspects are purely build-time in Metalama, so all semantics are build semantics.
    /// The equivalent interface is therefore <see cref="Metalama.Framework.Aspects.IAspect"/>.
    /// </summary>
    public interface IAspectBuildSemantics : IValidableAnnotation
    {
        /// <summary>
        /// Not supported in Metalama.
        /// </summary>
        AspectConfiguration GetAspectConfiguration( object targetElement );
    }
}