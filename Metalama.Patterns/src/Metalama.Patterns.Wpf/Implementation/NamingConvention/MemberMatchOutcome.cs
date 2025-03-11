// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Wpf.Implementation.NamingConvention;

[CompileTime]
internal enum MemberMatchOutcome
{
    /// <summary>
    /// A single eligible member matched, or a member can be introduced without conflict.
    /// </summary>
    Success,

    /// <summary>
    /// No eligible member matched.
    /// </summary>
    NotFound,

    /// <summary>
    /// Multiple eligible members matched.
    /// </summary>
    Ambiguous,

    /// <summary>
    /// The only matches found were invalid.
    /// </summary>
    Invalid,

    /// <summary>
    /// A member cannot be introduced because a member with the same name already exists.
    /// </summary>
    Conflict
}