// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// The kinds of <see cref="CallSiteRedirection"/>.
/// </summary>
internal enum CallSiteRedirectionKind
{
    /// <summary>
    /// The default value, which no <see cref="CallSiteRedirection"/> has.
    /// </summary>
    None,

    /// <summary>
    /// The redirection rewrites an invocation.
    /// </summary>
    Invocation,

    /// <summary>
    /// The redirection rewrites a method group that is converted to a delegate or to a function pointer.
    /// </summary>
    MethodReference
}
