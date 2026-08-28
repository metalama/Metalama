// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Threading;
using System;
using System.IO;
using System.IO.Compression;

namespace Metalama.Backstage.Tools;

internal sealed class BackstageToolsExtractor : IBackstageToolsExtractor
{
    private readonly IFileSystem _fileSystem;
    private readonly IBackstageToolsLocator _locator;
    private readonly INamedLockService _lockService;

    public BackstageToolsExtractor( IServiceProvider serviceProvider )
    {
        this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
        this._locator = serviceProvider.GetRequiredBackstageService<IBackstageToolsLocator>();
        this._lockService = serviceProvider.GetRequiredBackstageService<INamedLockService>();
    }

    private void Extract( BackstageTool tool )
    {
        var directory = this._locator.GetToolDirectory( tool );

        var touchFile = Path.Combine( directory, "unzipped.touch" );

        if ( !this._fileSystem.FileExists( touchFile ) )
        {
            using ( this._lockService.WithGlobalLock( touchFile ) )
            {
                if ( !this._fileSystem.FileExists( touchFile ) )
                {
                    this._fileSystem.CreateDirectory( directory );

                    var zipResourceName = $"Metalama.Backstage.Tools.{tool.Name}.zip";
                    var assembly = this.GetType().Assembly;
                    using var resourceStream = assembly.GetManifestResourceStream( zipResourceName );

                    if ( resourceStream == null )
                    {
                        throw new InvalidOperationException( $"Resource '{zipResourceName}' not found in '{assembly.Location}'." );
                    }

                    using var zipStream = new ZipArchive( resourceStream );
                    this._fileSystem.ExtractZipArchiveToDirectory( zipStream, directory );
                    this._fileSystem.WriteAllText( touchFile, "" );
                }
            }
        }
    }

    public void ExtractAll()
    {
        this.Extract( BackstageTool.Worker );
        this.Extract( BackstageTool.DesktopWindows );
    }
}