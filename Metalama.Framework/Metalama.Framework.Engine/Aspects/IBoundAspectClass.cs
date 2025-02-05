// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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