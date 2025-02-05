// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// Custom attribute that, when applied an an aspect class, means that this aspect class is implemented by a low-level weaver built with Metalama SDK.
/// When the <see cref="RequireAspectWeaverAttribute"/> is added to a type, the <see cref="IAspect{T}.BuildAspect"/> method is not invoked.
/// </summary>
[AttributeUsage( AttributeTargets.Class )]
[CompileTime]
[PublicAPI]
public sealed class RequireAspectWeaverAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequireAspectWeaverAttribute"/> class.
    /// </summary>
    /// <param name="weaverType">Full name (namespace and name but not assembly name) of the type implementing the aspect. This type must implement the
    /// <c>IAspectWeaver</c> interface and be annotated with the <c>[MetalamaPlugin]</c> attribute.</param>
    public RequireAspectWeaverAttribute( string weaverType )
    {
        this.Type = weaverType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireAspectWeaverAttribute"/> class.
    /// </summary>
    /// <param name="weaverType">The type implementing the aspect. This type must implement the
    /// <c>IAspectWeaver</c> interface and be annotated with the <c>[MetalamaPlugin]</c> attribute.</param>
    public RequireAspectWeaverAttribute( Type weaverType )
    {
        this.Type = weaverType.FullName!;
    }

    /// <summary>
    /// Gets the namespace-qualified name of the type implementing the aspect.
    /// </summary>
    public string Type { get; }
}