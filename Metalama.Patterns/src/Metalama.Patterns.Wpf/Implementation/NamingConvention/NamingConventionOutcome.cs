// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Wpf.Implementation.NamingConvention;

[CompileTime]
internal enum NamingConventionOutcome
{
    /// <summary>
    /// Both the primary and the secondary matches succeeded.
    /// </summary>
    Success,

    /// <summary>
    /// The primary match did not succeed, so another naming convention must be considered.
    /// </summary>
    Mismatch,

    /// <summary>
    /// The primary match succeeded but a secondary optional match failed.
    /// </summary>
    Warning,

    /// <summary>
    /// The primary match succeeded but a secondary required match failed.
    /// </summary>
    Error
}