// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using System;
using System.Diagnostics;
using System.IO;

namespace Metalama.Backstage.Tools;

internal class BackstageToolsExecutor : IBackstageToolsExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPlatformInfo _platformInfo;
    private readonly ILogger _logger;
    private readonly IProcessExecutor _processExecutor;
    private readonly IBackstageToolsLocator _locator;
    private readonly IFileSystem _fileSystem;

    public BackstageToolsExecutor( IServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._platformInfo = serviceProvider.GetRequiredBackstageService<IPlatformInfo>();
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "BackstageToolExecutor" );
        this._processExecutor = serviceProvider.GetRequiredBackstageService<IProcessExecutor>();
        this._locator = serviceProvider.GetRequiredBackstageService<IBackstageToolsLocator>();
        this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
    }

    public IProcess Start( BackstageTool tool, string arguments )
    {
        if ( this._locator.ToolsMustBeExtracted )
        {
            // This method can be called from different processes, including the Worker processes. These processes do not have 
            // the IBackstageToolsExtractor service, but of course they are guaranteed to run when tools have already been extracted.
            this._serviceProvider.GetBackstageService<IBackstageToolsExtractor>()?.ExtractAll();
        }

        var workerDirectory = this._locator.GetToolDirectory( tool );

        var programPath = Path.Combine( workerDirectory, $"{tool.Name}.{(tool.IsExe ? "exe" : "dll")}" );

        if ( !this._fileSystem.FileExists( programPath ) )
        {
            throw new FileNotFoundException( $"The file '{programPath}' does not exist.", programPath );
        }

        ProcessStartInfo processStartInfo;

        if ( tool.IsExe )
        {
            processStartInfo = new ProcessStartInfo()
            {
                FileName = programPath, Arguments = arguments, UseShellExecute = tool.UseShellExecute, WindowStyle = tool.WindowStyle
            };
        }
        else
        {
            var dotnetPath = this._platformInfo.DotNetExePath;
            var allArguments = $"\"{programPath}\" " + arguments;

            processStartInfo = new ProcessStartInfo()
            {
                FileName = dotnetPath, Arguments = allArguments, UseShellExecute = tool.UseShellExecute, WindowStyle = tool.WindowStyle
            };
        }

        this._logger.Info?.Log( $"Starting '{processStartInfo.FileName} {processStartInfo.Arguments}." );

        return this._processExecutor.Start( processStartInfo );
    }
}