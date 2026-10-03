// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Engine.Extensibility.Transformations;

/// <summary>
/// Represents the method that replaces a call site.
/// </summary>
[PublicAPI]
public sealed class CallSiteRedirectionTarget
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CallSiteRedirectionTarget"/> class. Use <see cref="Existing"/> to create an instance.
    /// </summary>
    private CallSiteRedirectionTarget( IMethod method, INamedType? containingTypeAtCallSite )
    {
        this.Method = method;
        this.ContainingTypeAtCallSite = containingTypeAtCallSite;
    }

    /// <summary>
    /// Creates a target that is an existing method of the current compilation or of a referenced assembly.
    /// </summary>
    /// <param name="method">The method. It must be static.</param>
    /// <param name="containingTypeAtCallSite">The containing type as it must be written at the call site, for example a constructed generic type.
    /// The default is the declaring type of <paramref name="method"/>.</param>
    public static CallSiteRedirectionTarget Existing( IMethod method, INamedType? containingTypeAtCallSite = null )
        => new( method ?? throw new ArgumentNullException( nameof(method) ), containingTypeAtCallSite );

    /// <summary>
    /// Gets the method.
    /// </summary>
    public IMethod Method { get; }

    /// <summary>
    /// Gets the containing type as it must be written at the call site, or <c>null</c> for the declaring type of <see cref="Method"/>.
    /// </summary>
    public INamedType? ContainingTypeAtCallSite { get; }
}
