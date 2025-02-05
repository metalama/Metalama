// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.DesignTime.Pipeline.Diff;

internal readonly record struct CompilationChangesHandle( CompilationChanges? Value, ProjectVersion? OldProjectVersion );