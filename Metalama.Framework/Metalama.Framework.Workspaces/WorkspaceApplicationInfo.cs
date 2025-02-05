// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;

namespace Metalama.Framework.Workspaces;

internal sealed class WorkspaceApplicationInfo : ApplicationInfoBase
{
    public WorkspaceApplicationInfo() : base( typeof(WorkspaceApplicationInfo).Assembly ) { }

    public override string Name => "Metalama.Workspace";
}