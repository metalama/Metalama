// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;

namespace Metalama.Backstage.Tools;

public interface IBackstageToolsExecutor : IBackstageService
{
    IProcess Start( BackstageTool tool, string arguments );
}

public interface IBackstageToolsLocator : IBackstageService
{
    bool ToolsMustBeExtracted { get; }

    string GetToolDirectory( BackstageTool tool );
}