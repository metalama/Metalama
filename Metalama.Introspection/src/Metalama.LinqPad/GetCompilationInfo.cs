// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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