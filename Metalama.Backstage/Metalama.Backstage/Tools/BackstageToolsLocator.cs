// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Maintenance;
using System;

namespace Metalama.Backstage.Tools;

internal sealed class BackstageToolsLocator : IBackstageToolsLocator
{
    private readonly ITempFileManager _tempFileManager;

    public BackstageToolsLocator( IServiceProvider serviceProvider )
    {
        this._tempFileManager = serviceProvider.GetRequiredBackstageService<ITempFileManager>();
    }

    public bool ToolsMustBeExtracted => true;

    public string GetToolDirectory( BackstageTool tool )
        => this._tempFileManager.GetTempDirectory(
            "Tools",
            cleanUpStrategy: CleanUpStrategy.WhenUnused,
            subdirectory: tool.Name,
            TempFileVersionScope.Backstage );
}