// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Metalama.Backstage.Tools;

internal sealed class BackstageToolsExecutor : IBackstageToolsExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPlatformInfo _platformInfo;
    private readonly ILogger _logger;
    private readonly IProcessExecutor _processExecutor;
    private readonly IBackstageToolsLocator _locator;
    private readonly IFileSystem _fileSystem;

    private static readonly char[] _charactersRequiringQuoting = { ' ', '\t', '\n', '\v', '"' };

    public BackstageToolsExecutor( IServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._platformInfo = serviceProvider.GetRequiredBackstageService<IPlatformInfo>();
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "BackstageToolExecutor" );
        this._processExecutor = serviceProvider.GetRequiredBackstageService<IProcessExecutor>();
        this._locator = serviceProvider.GetRequiredBackstageService<IBackstageToolsLocator>();
        this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
    }

    public IProcess Start( BackstageTool tool, params string[] arguments )
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
                FileName = programPath, Arguments = FormatArguments( arguments ), UseShellExecute = tool.UseShellExecute, WindowStyle = tool.WindowStyle
            };
        }
        else
        {
            var dotnetPath = this._platformInfo.DotNetExePath;

            // The program path becomes the first argument passed to dotnet.exe.
            var allArguments = new List<string>( arguments.Length + 1 ) { programPath };
            allArguments.AddRange( arguments );

            processStartInfo = new ProcessStartInfo()
            {
                FileName = dotnetPath, Arguments = FormatArguments( allArguments ), UseShellExecute = tool.UseShellExecute, WindowStyle = tool.WindowStyle
            };
        }

        this._logger.Info?.Log( $"Starting '{processStartInfo.FileName} {processStartInfo.Arguments}." );

        return this._processExecutor.Start( processStartInfo );
    }

    /// <summary>
    /// Joins an argument vector into a single command-line string, quoting each argument as required so that it
    /// round-trips through the Windows <c>CommandLineToArgvW</c> parsing rules. This prevents untrusted argument
    /// values from injecting additional arguments. Equivalent to the .NET <c>PasteArguments</c> implementation,
    /// which we cannot use directly because it is internal and because <c>ProcessStartInfo.ArgumentList</c>
    /// is not available on all target frameworks.
    /// </summary>
    private static string FormatArguments( IReadOnlyList<string> arguments )
    {
        var builder = new StringBuilder();

        foreach ( var argument in arguments )
        {
            AppendArgument( builder, argument );
        }

        return builder.ToString();
    }

    private static void AppendArgument( StringBuilder builder, string argument )
    {
        if ( builder.Length != 0 )
        {
            builder.Append( ' ' );
        }

        // An argument with no whitespace or quote can be appended verbatim.
        if ( argument.Length != 0 && argument.IndexOfAny( _charactersRequiringQuoting ) < 0 )
        {
            builder.Append( argument );

            return;
        }

        builder.Append( '"' );

        var index = 0;

        while ( index < argument.Length )
        {
            var c = argument[index++];

            if ( c == '\\' )
            {
                var backslashCount = 1;

                while ( index < argument.Length && argument[index] == '\\' )
                {
                    index++;
                    backslashCount++;
                }

                if ( index == argument.Length )
                {
                    // Backslashes immediately preceding the closing quote must be doubled.
                    builder.Append( '\\', backslashCount * 2 );
                }
                else if ( argument[index] == '"' )
                {
                    // Backslashes preceding a quote must be doubled, plus one to escape the quote itself.
                    builder.Append( '\\', ( backslashCount * 2 ) + 1 );
                    builder.Append( '"' );
                    index++;
                }
                else
                {
                    builder.Append( '\\', backslashCount );
                }
            }
            else if ( c == '"' )
            {
                builder.Append( '\\' );
                builder.Append( '"' );
            }
            else
            {
                builder.Append( c );
            }
        }

        builder.Append( '"' );
    }
}