// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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