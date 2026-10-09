// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Extensibility.Synthesis;
using System;

namespace Metalama.Framework.Engine.Extensibility.CallSites;

/// <summary>
/// Represents the method that replaces a call site. The implementations are <see cref="ExistingCallSiteRedirectionTarget"/> and
/// <see cref="SynthesizedCallSiteRedirectionTarget"/>, and <see cref="Kind"/> tells them apart.
/// </summary>
[PublicAPI]
public abstract class CallSiteRedirectionTarget
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CallSiteRedirectionTarget"/> class.
    /// </summary>
    private protected CallSiteRedirectionTarget( IMethod method, INamedType? containingTypeAtCallSite )
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
    public static ExistingCallSiteRedirectionTarget Existing( IMethod method, INamedType? containingTypeAtCallSite = null )
        => new( method ?? throw new ArgumentNullException( nameof(method) ), containingTypeAtCallSite );

    /// <summary>
    /// Creates a target that is a method declared with <see cref="ExtensionTransformationFactory.DeclareMethod"/>.
    /// </summary>
    /// <param name="method">The handle of the declared method. A static method can be called from any call site from which it is accessible. An
    /// instance method can be called only from an instance member of its declaring type or of a type derived from it, where <c>this</c> is
    /// available, and it is called on <c>this</c>.</param>
    /// <param name="containingTypeAtCallSite">The containing type as it must be written at the call site, for example a constructed generic type.
    /// The default is the declaring type of the method.</param>
    /// <remarks>
    /// When the template of the method fails to expand, the linker leaves the call sites redirected to the method unchanged.
    /// </remarks>
    public static SynthesizedCallSiteRedirectionTarget Synthesized( SynthesizedMethodHandle method, INamedType? containingTypeAtCallSite = null )
        => new( method ?? throw new ArgumentNullException( nameof(method) ), containingTypeAtCallSite );

    /// <summary>
    /// Gets the kind of the target, which tells the implementations apart.
    /// </summary>
    public abstract CallSiteRedirectionTargetKind Kind { get; }

    /// <summary>
    /// Gets the method.
    /// </summary>
    public IMethod Method { get; }

    /// <summary>
    /// Gets the containing type as it must be written at the call site, or <c>null</c> for the declaring type of <see cref="Method"/>.
    /// </summary>
    public INamedType? ContainingTypeAtCallSite { get; }
}
