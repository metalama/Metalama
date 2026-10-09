// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Extensibility.Synthesis;

namespace Metalama.Framework.Engine.Extensibility.CallSites;

/// <summary>
/// A <see cref="CallSiteRedirectionTarget"/> that is a method declared with <see cref="ExtensionTransformationFactory.DeclareMethod"/>. Create
/// an instance with <see cref="CallSiteRedirectionTarget.Synthesized"/>.
/// </summary>
[PublicAPI]
public sealed class SynthesizedCallSiteRedirectionTarget : CallSiteRedirectionTarget
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SynthesizedCallSiteRedirectionTarget"/> class.
    /// </summary>
    internal SynthesizedCallSiteRedirectionTarget( SynthesizedMethodHandle handle, INamedType? containingTypeAtCallSite ) : base(
        handle.Method,
        containingTypeAtCallSite )
    {
        this.Handle = handle;
    }

    /// <inheritdoc />
    public override CallSiteRedirectionTargetKind Kind => CallSiteRedirectionTargetKind.Synthesized;

    /// <summary>
    /// Gets the handle of the declared method.
    /// </summary>
    public SynthesizedMethodHandle Handle { get; }
}
