// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Framework.Engine.Extensibility.CallSites;

/// <summary>
/// Kinds of <see cref="CallSiteRedirectionTarget"/>.
/// </summary>
[PublicAPI]
public enum CallSiteRedirectionTargetKind
{
    /// <summary>
    /// An existing method, represented by <see cref="ExistingCallSiteRedirectionTarget"/>.
    /// </summary>
    Existing,

    /// <summary>
    /// A method declared with <see cref="ExtensionTransformationFactory.DeclareMethod"/>, represented by
    /// <see cref="SynthesizedCallSiteRedirectionTarget"/>.
    /// </summary>
    Synthesized
}
