// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.Extensibility.CallSites;

/// <summary>
/// A <see cref="CallSiteRedirectionTarget"/> that is an existing static method of the current compilation or of a referenced assembly. Create
/// an instance with <see cref="CallSiteRedirectionTarget.Existing"/>.
/// </summary>
[PublicAPI]
public sealed class ExistingCallSiteRedirectionTarget : CallSiteRedirectionTarget
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExistingCallSiteRedirectionTarget"/> class.
    /// </summary>
    internal ExistingCallSiteRedirectionTarget( IMethod method, INamedType? containingTypeAtCallSite ) : base( method, containingTypeAtCallSite ) { }

    /// <inheritdoc />
    public override CallSiteRedirectionTargetKind Kind => CallSiteRedirectionTargetKind.Existing;
}
