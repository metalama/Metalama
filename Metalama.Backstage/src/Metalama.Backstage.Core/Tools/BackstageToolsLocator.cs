// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Maintenance;
using System;

namespace Metalama.Backstage.Tools;

internal sealed class BackstageToolsLocator : IBackstageToolsLocator
{
    private readonly ITempFileManager _tempFileManager;
    private readonly ProductProfile _productProfile;

    public BackstageToolsLocator( IServiceProvider serviceProvider )
    {
        this._tempFileManager = serviceProvider.GetRequiredBackstageService<ITempFileManager>();
        this._productProfile = serviceProvider.GetRequiredBackstageService<ProductProfile>();
    }

    public bool ToolsMustBeExtracted => true;

    public string GetToolDirectory( BackstageTool tool )
        => this._tempFileManager.GetTempDirectory(
            "Tools",
            cleanUpStrategy: CleanUpStrategy.WhenUnused,
            subdirectory: tool.GetAssemblyName( this._productProfile ),
            TempFileVersionScope.Backstage );
}