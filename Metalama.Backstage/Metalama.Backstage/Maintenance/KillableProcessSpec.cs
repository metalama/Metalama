// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.Maintenance;

internal readonly record struct KillableProcessSpec( string Name, KillableModuleKind Kind, bool CanShutdown, bool CanKill, string? DisplayName = null )
{
    public bool IsDotNet => (this.Kind & KillableModuleKind.DotNet) != 0;

    public bool IsStandaloneProcess => (this.Kind & KillableModuleKind.StandaloneProcess) != 0;

    public bool CanShutdownOrKill => this.CanShutdown || this.CanKill;
}