// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.AspectWeavers
{
    /// <summary>
    /// Aspect drivers are responsible for executing aspects.
    /// </summary>
    /// <remarks>
    /// There are low-level aspect drivers, which should implement <see cref="IAspectWeaver"/>, and a high-level aspect driver implemented
    /// by Metalama. These two families of drivers don't share any semantic. This interface exists for clarity and type safety only.
    /// </remarks>
    public interface IAspectDriver;
}