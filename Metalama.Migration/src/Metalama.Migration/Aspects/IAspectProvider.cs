// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using PostSharp.Extensibility;
using System.Collections.Generic;

namespace PostSharp.Aspects
{
    /// <summary>
    /// In Metalama, you can add aspects using fabrics. In aspects, implement the <see cref="IAspect{T}.BuildAspect"/>.
    /// In fabrics, implement <see cref="TypeFabric.AmendType"/>, <see cref="ProjectFabric.AmendProject"/> or <see cref="NamespaceFabric.AmendNamespace"/>.
    /// In both cases,
    /// call the <see cref="IAspectReceiverSelector{TTarget}.With{TMember}(System.Func{TTarget,System.Collections.Generic.IEnumerable{TMember}})"/>
    /// method, then call <see cref="IAspectReceiver{TDeclaration}.AddAspect{TAspect}()"/>.
    /// </summary>
    /// <seealso href="@child-aspects"/>
    /// <seealso href="@fabrics-aspects"/>
    public interface IAspectProvider : IAspect, IService
    {
        IEnumerable<AspectInstance> ProvideAspects( object targetElement );
    }
}