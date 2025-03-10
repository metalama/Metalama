// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.AspectWeavers;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Aspects
{
    /// <summary>
    /// An <see cref="IAspectClass"/> for which an <see cref="IAspectDriver"/> has been created.
    /// </summary>
    internal interface IBoundAspectClass : IAspectClassImpl
    {
        IAspectDriver AspectDriver { get; }

        Location? GetDiagnosticLocation( Compilation compilation );
    }
}