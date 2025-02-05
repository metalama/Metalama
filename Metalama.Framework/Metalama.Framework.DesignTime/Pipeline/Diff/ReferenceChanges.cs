// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline.Diff;

/// <summary>
/// Represents changes in compilation references.
/// </summary>
internal record struct ReferenceChanges(
    ImmutableDictionary<ProjectKey, IProjectVersion> NewProjectReferences,
    ImmutableDictionary<ProjectKey, ReferencedProjectChange> ProjectReferenceChanges,
    ImmutableHashSet<string> NewPortableExecutableReferences,
    ImmutableDictionary<string, ReferenceChangeKind> PortableExecutableReferenceChanges );