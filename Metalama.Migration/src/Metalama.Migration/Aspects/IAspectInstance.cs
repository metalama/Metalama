// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using PostSharp.Aspects.Configuration;
using PostSharp.Constraints;
using PostSharp.Reflection;
using System;

namespace PostSharp.Aspects
{
    /// <summary>
    /// Use <see cref="Metalama.Framework.Aspects.IAspectInstance"/>.
    /// </summary>
    [InternalImplement]
    public interface IAspectInstance
    {
        /// <summary>
        /// There is no aspect configuration in Metalama.
        /// </summary>
        AspectConfiguration AspectConfiguration { get; }

        /// <summary>
        /// Use <see cref="Metalama.Framework.Aspects.IAspectPredecessor.Predecessors"/>/
        /// </summary>
        ObjectConstruction AspectConstruction { get; }

        IAspect Aspect { get; }

        Type AspectType { get; }
    }
}