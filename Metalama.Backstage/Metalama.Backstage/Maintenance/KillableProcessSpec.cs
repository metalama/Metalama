// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Maintenance;

internal readonly record struct KillableProcessSpec( string Name, KillableModuleKind Kind, bool CanShutdown, bool CanKill, string? DisplayName = null )
{
    public bool IsDotNet => (this.Kind & KillableModuleKind.DotNet) != 0;

    public bool IsStandaloneProcess => (this.Kind & KillableModuleKind.StandaloneProcess) != 0;

    public bool CanShutdownOrKill => this.CanShutdown || this.CanKill;
}