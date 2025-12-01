// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Eligibility;
using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// Binds an aspect class to a low-level weaver implementation built with <c>Metalama.Framework.Sdk</c>.
/// When this attribute is applied to an aspect class, the <see cref="IAspect{T}.BuildAspect"/> method is bypassed
/// and the specified weaver handles all transformations instead.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute when you need to perform C# code transformations that are not possible with the standard
/// <see cref="IAspectBuilder"/> advice API and require direct access to the Roslyn API.
/// </para>
/// <para>
/// The weaver type must:
/// </para>
/// <list type="bullet">
/// <item><description>Implement <c>IAspectWeaver</c> from the <c>Metalama.Framework.Engine.AspectWeavers</c> namespace.</description></item>
/// <item><description>Be annotated with <c>[MetalamaPlugIn]</c>.</description></item>
/// <item><description>Have a public parameterless constructor.</description></item>
/// </list>
/// <para>
/// Although <see cref="IAspect{T}.BuildAspect"/> is not called, <see cref="IEligible{T}.BuildEligibility"/>
/// is still invoked, allowing you to define eligibility rules as usual.
/// </para>
/// <para>
/// <b>Warning:</b> Weaver-based aspects are significantly more complex to implement, have worse IDE integration,
/// and have a significant performance impact. Prefer the standard aspect approach when possible.
/// </para>
/// </remarks>
/// <seealso href="@aspect-weavers"/>
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