// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using PostSharp.Aspects.Configuration;
using PostSharp.Reflection;

namespace PostSharp.Aspects
{
    /// <summary>
    /// In PostSharp, this class was used with <see cref="IAspectProvider"/>. In Metalama, no additional object is necessary when adding
    /// an aspect instance. Read-only access to aspect instances is provided through the <see cref="IAspectInstance"/> interface.
    /// </summary>
    [PublicAPI]
    public sealed class AspectInstance : AspectSpecification
    {
        public AspectInstance( object targetElement, IAspect aspect ) { }

        public AspectInstance( object targetElement, IAspect aspect, AspectConfiguration aspectConfiguration ) { }

        public AspectInstance( object targetElement, ObjectConstruction aspectConstruction ) { }

        public AspectInstance(
            object targetElement,
            ObjectConstruction aspectConstruction,
            AspectConfiguration aspectConfiguration ) { }

        public object TargetElement { get; }

        public bool RepresentAsStandalone { get; set; }
    }
}