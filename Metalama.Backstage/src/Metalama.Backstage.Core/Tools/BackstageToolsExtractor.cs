// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Threading;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace Metalama.Backstage.Tools;

/// <summary>
/// The implementation of <see cref="IBackstageToolsExtractor"/> that extracts the tool applications from the zip
/// archives embedded in an assembly. The archive of a tool is the resource whose name ends with the assembly name of
/// the tool followed by <c>.zip</c>, so the assembly of any product can hold them under its own resource prefix.
/// </summary>
public sealed class BackstageToolsExtractor : IBackstageToolsExtractor
{
    private readonly IFileSystem _fileSystem;
    private readonly IBackstageToolsLocator _locator;
    private readonly INamedLockService _lockService;
    private readonly ProductProfile _productProfile;
    private readonly Assembly _resourceAssembly;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackstageToolsExtractor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="resourceAssembly">The assembly that embeds the zip archives of the tools.</param>
    public BackstageToolsExtractor( IServiceProvider serviceProvider, Assembly resourceAssembly )
    {
        this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
        this._locator = serviceProvider.GetRequiredBackstageService<IBackstageToolsLocator>();
        this._lockService = serviceProvider.GetRequiredBackstageService<INamedLockService>();
        this._productProfile = serviceProvider.GetRequiredBackstageService<ProductProfile>();
        this._resourceAssembly = resourceAssembly;
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

                    var zipResourceSuffix = $".{tool.GetAssemblyName( this._productProfile )}.zip";

                    var zipResourceName = this._resourceAssembly.GetManifestResourceNames()
                                              .SingleOrDefault( name => name.EndsWith( zipResourceSuffix, StringComparison.Ordinal ) )
                                          ?? throw new InvalidOperationException(
                                              $"No resource named '*{zipResourceSuffix}' was found in '{this._resourceAssembly.Location}'." );

                    using var resourceStream = this._resourceAssembly.GetManifestResourceStream( zipResourceName )
                                               ?? throw new InvalidOperationException(
                                                   $"Resource '{zipResourceName}' not found in '{this._resourceAssembly.Location}'." );

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
