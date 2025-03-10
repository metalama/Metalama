// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// Custom attribute that, when applied to an aspect class, means that this aspect class uses several
/// layers, and defines the name and the order of execution of these layers. In multi-aspect layers,
/// the <see cref="IAspect{T}.BuildAspect"/> method is called several times, once for each layer.
/// The current layer is exposed in the <see cref="IAspectBuilder.Layer"/> property.
/// </summary>
[AttributeUsage( AttributeTargets.Class )]
[PublicAPI]
public sealed class LayersAttribute : Attribute
{
    public string[] Layers { get; }

    public LayersAttribute( params string[] layers )
    {
        this.Layers = layers;
    }
}