// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.DesignTime.Pipeline.Diff;

/// <summary>
/// Kinds of <see cref="ReferencedProjectChange"/>.
/// </summary>
internal enum ReferenceChangeKind
{
    None,
    Added,
    Removed,
    Modified
}