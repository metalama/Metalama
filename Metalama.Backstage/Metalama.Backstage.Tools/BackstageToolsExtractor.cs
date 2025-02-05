// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Utilities;
using System;
using System.IO;
using System.IO.Compression;

namespace Metalama.Backstage.Tools;

internal class BackstageToolsExtractor : IBackstageToolsExtractor
{
    private readonly IFileSystem _fileSystem;
    private readonly IBackstageToolsLocator _locator;

    public BackstageToolsExtractor( IServiceProvider serviceProvider )
    {
        this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
        this._locator = serviceProvider.GetRequiredBackstageService<IBackstageToolsLocator>();
    }

    private void Extract( BackstageTool tool )
    {
        var directory = this._locator.GetToolDirectory( tool );

        var touchFile = Path.Combine( directory, "unzipped.touch" );

        if ( !this._fileSystem.FileExists( touchFile ) )
        {
            using ( MutexHelper.WithGlobalLock( touchFile ) )
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