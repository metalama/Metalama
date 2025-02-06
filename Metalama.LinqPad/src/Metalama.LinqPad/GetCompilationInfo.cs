// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.LinqPad;

internal readonly struct GetCompilationInfo
{
    public string WorkspaceExpression { get; }

    public bool IsMetalamaOutput { get; }

    public GetCompilationInfo( string workspaceExpression, bool isMetalamaOutput )
    {
        this.WorkspaceExpression = workspaceExpression;
        this.IsMetalamaOutput = isMetalamaOutput;
    }
}