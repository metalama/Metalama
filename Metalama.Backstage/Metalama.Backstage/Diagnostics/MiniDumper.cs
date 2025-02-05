// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Backstage.Diagnostics;

internal class MiniDumper : IMiniDumper
{
    private static int _isAppDomainInitialized;
    private static volatile WeakReference<MiniDumper>? _latestDumper;

    private readonly ILogger _logger;
    private readonly CrashDumpConfiguration _configuration;
    private readonly bool _isProcessEnabled;
    private readonly ProcessKind _processKind;
    private readonly ITempFileManager _tempFileManager;
    private readonly IPlatformInfo _platformInfo;

    public MiniDumper( IServiceProvider serviceProvider )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "Dumper" );
        this._configuration = serviceProvider.GetRequiredBackstageService<IConfigurationManager>().Get<DiagnosticsConfiguration>().CrashDumps;
        this._tempFileManager = serviceProvider.GetRequiredBackstageService<ITempFileManager>();
        this._platformInfo = serviceProvider.GetRequiredBackstageService<IPlatformInfo>();

        var applicationInfo = serviceProvider.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication;
        this._processKind = applicationInfo.ProcessKind;
        this._isProcessEnabled = this._configuration.Processes.TryGetValue( applicationInfo.ProcessKind.ToString(), out var isEnabled ) && isEnabled;

        // The MiniDumper class is instantiated for each project, but the handler must be global.
        // We will use the latest available configuration in the process.
        if ( this._isProcessEnabled )
        {
            if ( Interlocked.Exchange( ref _isAppDomainInitialized, 1 ) == 0 )
            {
                AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
            }
        }

        _latestDumper = new WeakReference<MiniDumper>( this );
    }

    private static void OnFirstChanceException( object? sender, FirstChanceExceptionEventArgs e )
    {
        var dumperRef = _latestDumper;

        if ( dumperRef == null || !dumperRef.TryGetTarget( out var dumper ) )
        {
            return;
        }

        if ( dumper.MustWrite( e.Exception ) )
        {
            dumper.Write();
        }
    }

    public bool MustWrite( Exception exception )
        => this._isProcessEnabled
           && exception is not (TaskCanceledException or OperationCanceledException or IOException or WebException)
           && !(exception is AggregateException { InnerExceptions.Count: 1 } aggregateException
                && !this.MustWrite( aggregateException.InnerException! ))
           && (this._configuration.ExceptionTypes.Contains( exception.GetType().Name ) || this._configuration.ExceptionTypes.Contains( "*" ));

    public string? Write( MiniDumpOptions? options = null )
    {
        options ??= MiniDumpOptions.Default;

        using ( MutexHelper.WithGlobalLock( "MiniDump" ) )
        {
            try
            {
                var directory = this._tempFileManager.GetTempDirectory( "CrashReports", CleanUpStrategy.Always );

                var fileName = Path.Combine( directory, $"{this._processKind.ToString().ToLowerInvariant()}-{Guid.NewGuid()}.dmp" );

                this._logger.Info?.Log( $"Saving a dump to '{fileName}.'" );

                if ( !ToolInvocationHelper.InvokeTool(
                        this._logger,
                        this._platformInfo.DotNetExePath,
                        $"dump collect -p {Process.GetCurrentProcess().Id} -o \"{fileName}\"",
                        null ) )
                {
                    return null;
                }

                if ( !options.Compress )
                {
                    return fileName;
                }
                else
                {
                    var compressedFileName = fileName + ".gz";

                    this._logger.Info?.Log( $"Compressing dump to '{compressedFileName}.'" );

                    using ( var readStream = File.OpenRead( fileName ) )
                    using ( var writeStream = new GZipStream( File.OpenWrite( compressedFileName ), CompressionMode.Compress ) )
                    {
                        readStream.CopyTo( writeStream );
                    }

                    File.Delete( fileName );

                    return compressedFileName;
                }
            }
            catch ( Exception e )
            {
                this._logger.Error?.Log( e.ToString() );

                return null;
            }
        }
    }
}