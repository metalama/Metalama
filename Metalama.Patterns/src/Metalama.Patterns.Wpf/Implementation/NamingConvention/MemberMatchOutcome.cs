// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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