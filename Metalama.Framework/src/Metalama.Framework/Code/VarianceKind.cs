// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code;

/// <summary>
/// Specifies the kind variance: <see cref="In"/>, <see cref="Out"/> or <see cref="None"/>.
/// </summary>
[CompileTime]
public enum VarianceKind
{
    /// <summary>
    /// No variance.
    /// </summary>
    None,

    /// <summary>
    /// Contravariant.
    /// </summary>
    In,

    /// <summary>
    /// Covariant.
    /// </summary>
    Out
}