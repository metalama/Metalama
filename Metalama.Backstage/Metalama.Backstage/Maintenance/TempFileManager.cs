// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Utilities;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Metalama.Backstage.Maintenance;

public sealed class TempFileManager : ITempFileManager
{
    private const string _cleanUpFileName = "cleanup.json";

    private readonly CleanUpConfiguration _configuration;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly IStandardDirectories _standardDirectories;
    private readonly IDateTimeProvider _time;
    private readonly IConfigurationManager _configurationManager;
    private readonly BackstageBackgroundTasksService _backgroundTasksService;
    private readonly string _applicationVersion;
    private readonly string _backstageVersion;

    public TempFileManager( IServiceProvider serviceProvider )
    {
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
        this._configuration = this._configurationManager.Get<CleanUpConfiguration>();
        this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
        this._logger = serviceProvider.GetRequiredBackstageService<EarlyLoggerFactory>().GetLogger( "Configuration" );
        this._time = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._standardDirectories = serviceProvider.GetRequiredBackstageService<IStandardDirectories>();
        this._backgroundTasksService = serviceProvider.GetRequiredBackstageService<BackstageBackgroundTasksService>();

        var application = serviceProvider.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication;

        if ( !AssemblyMetadataReader.GetInstance( typeof(TempFileManager).Assembly ).TryGetValue( "BackstagePackageVersion", out var backstageVersion ) )
        {
            throw new InvalidOperationException();
        }

        this._backstageVersion = backstageVersion;

        this._applicationVersion = application.GetLatestComponentMadeByPostSharp().PackageVersion ??
                                   throw new InvalidOperationException( "The application version is not set." );
    }

    /// <summary>
    /// Cleans Metalama cache subdirectories, conforming the policies set in <c>cleanup.json</c> for each directory. This method gets automatically called from compiler as well.
    /// </summary>
    /// <param name="force">Ignore last clean-up time.</param>
    public void CleanTempDirectories( bool force = false, bool all = false )
    {
        using var mutex = MutexHelper.WithLock(
            this._standardDirectories.TempDirectory,
            this._fileSystem.SynchronizationPrefix,
            TimeSpan.FromMilliseconds( 1 ),
            this._logger );

        if ( mutex == null )
        {
            this._logger.Warning?.Log( "Clean-up is already running." );

            return;
        }

        try
        {
            var now = this._time.UtcNow;
            var lastCleanUpTime = this._configuration.LastCleanUpTime;
            var nextCleanUpTime = lastCleanUpTime?.AddDays( 1 );

            if ( !force &&
                 lastCleanUpTime != null &&
                 now < nextCleanUpTime )
            {
                this._logger.Info?.Log(
                    $"It's not time to clean up cache directories yet. Next clean-up time: {nextCleanUpTime}. Use --force to override this condition." );

                return;
            }

            // Go through all cache directories in temp directory (i.e. CrashReports, ExtractExceptions, Logs etc.)
            this.CleanUpDirectory( this._standardDirectories.TempDirectory, all );
        }
        finally
        {
            mutex.Dispose();
            this._configurationManager.Update<CleanUpConfiguration>( c => c with { LastCleanUpTime = this._time.UtcNow } );
        }
    }

    private void CleanUpDirectory( string directory, bool all )
    {
        if ( !this._fileSystem.DirectoryExists( directory ) )
        {
            return;
        }

        var cleanUpAction = this.GetCleanUpAction( directory, all );

        switch ( cleanUpAction )
        {
            case CleanUpAction.CleanUpSubdirectories:
                foreach ( var subdirectory in this._fileSystem.EnumerateDirectories( directory ) )
                {
                    this.CleanUpDirectory( subdirectory, all );

                    // If the directory became empty, we delete it.
                    if ( this._fileSystem.IsDirectoryEmpty( directory ) )
                    {
                        this.DeleteDirectory( directory, true );
                    }
                }

                break;

            case CleanUpAction.DeleteDirectory:
                this.DeleteDirectory( directory, false );

                break;

            case CleanUpAction.MoveAndDeleteDirectory:
                this.DeleteDirectory( directory, true );

                break;

            case CleanUpAction.Delete4HoursOldFilesFirst:
                this.DeleteIndividualFiles( directory, TimeSpan.FromHours( 4 ) );

                break;

            case CleanUpAction.DeleteOneMonthOldFilesFirst:
                this.DeleteIndividualFiles( directory, TimeSpan.FromDays( 30 ) );

                break;

            default:
                throw new InvalidOperationException( $"Unknown clean-up action '{cleanUpAction}'." );
        }
    }

    private CleanUpAction GetCleanUpAction( string directory, bool all )
    {
        var cleanUpFilePath = Path.Combine( directory, _cleanUpFileName );

        if ( !this._fileSystem.FileExists( cleanUpFilePath ) )
        {
            if ( !all )
            {
                return CleanUpAction.CleanUpSubdirectories;
            }
            else
            {
                // When we delete all files, we only delete leave directories, so we delete them depth-first.
                return this._fileSystem.GetDirectories( directory ).Any() ? CleanUpAction.CleanUpSubdirectories : CleanUpAction.MoveAndDeleteDirectory;
            }
        }

        if ( all )
        {
            return CleanUpAction.MoveAndDeleteDirectory;
        }
        else
        {
            var jsonFileContent = this._fileSystem.ReadAllText( cleanUpFilePath );

            CleanUpFile? cleanUpFile;

            try
            {
                cleanUpFile = JsonConvert.DeserializeObject<CleanUpFile>( jsonFileContent );
            }
            catch ( JsonException e )
            {
                this._logger.Error?.Log( $"Cannot deserialize '{jsonFileContent}' from '{cleanUpFilePath}': {e.Message}" );

                return CleanUpAction.CleanUpSubdirectories;
            }

            if ( cleanUpFile == null )
            {
                this._logger.Error?.Log( $"Cannot deserialize '{jsonFileContent}' from '{cleanUpFilePath}': the file is empty." );

                return CleanUpAction.CleanUpSubdirectories;
            }

            switch ( cleanUpFile.Strategy )
            {
                case CleanUpStrategy.None:
                    this._logger.Trace?.Log(
                        $"The '{directory}' directory clean-up strategy has been set to '{nameof(CleanUpStrategy.None)}'. The directory will not be deleted." );

                    return CleanUpAction.CleanUpSubdirectories;

                case CleanUpStrategy.Always:
                    this._logger.Trace?.Log(
                        $"The '{directory}' directory clean-up strategy has been set to '{nameof(CleanUpStrategy.Always)}'. The individual files in the directory will be cleaned up." );

                    return CleanUpAction.Delete4HoursOldFilesFirst;

                case CleanUpStrategy.AlwaysNoMove:
                    this._logger.Trace?.Log(
                        $"The '{directory}' directory clean-up strategy has been set to '{nameof(CleanUpStrategy.AlwaysNoMove)}'. This is a new location of a directory that has previously failed to delete. The directory will be deleted." );

                    return CleanUpAction.DeleteDirectory;

                case CleanUpStrategy.WhenUnused:
                    var lastWriteTime = this._fileSystem.GetFileLastWriteTime( cleanUpFilePath );
                    var lastWriteTimeUtc = lastWriteTime.ToUniversalTime();
                    const int days = 7;

                    if ( lastWriteTimeUtc < this._time.UtcNow.AddDays( -days ) )
                    {
                        this._logger.Trace?.Log(
                            $"The '{directory}' directory clean-up strategy has been set to '{nameof(CleanUpStrategy.WhenUnused)}' and the directory hasn't been used for more than {days} days since {lastWriteTime:s}. The directory will be deleted." );

                        return CleanUpAction.MoveAndDeleteDirectory;
                    }
                    else
                    {
                        this._logger.Trace?.Log(
                            $"The '{directory}' directory clean-up strategy has been set to '{nameof(CleanUpStrategy.WhenUnused)}' and the directory hasn't been used for less than {days} days since {lastWriteTime:s}. The directory will not be deleted." );

                        return CleanUpAction.CleanUpSubdirectories;
                    }

                case CleanUpStrategy.FileOneMonthAfterCreation:
                    this._logger.Trace?.Log(
                        $"The '{directory}' directory clean-up strategy has been set to '{nameof(CleanUpStrategy.FileOneMonthAfterCreation)}'. The individual files in the directory will be cleaned up." );

                    return CleanUpAction.DeleteOneMonthOldFilesFirst;

                default:
                    this._logger.Warning?.Log(
                        $"The '{directory}' directory clean-up strategy '{cleanUpFile.Strategy}' is unknown. The directory will not be deleted." );

                    return CleanUpAction.CleanUpSubdirectories;
            }
        }
    }

    private void DeleteDirectory( string directory, bool moveFirst )
    {
        if ( moveFirst )
        {
            if ( !this.TryMoveDirectory( directory, out directory! ) )
            {
                return;
            }
        }

        this._logger.Trace?.Log( $"Deleting '{directory}' directory." );

        try
        {
            try
            {
                this._fileSystem.DeleteDirectory( directory, true );
            }
            catch ( UnauthorizedAccessException )
            {
                this._logger.Trace?.Log( $"Some files in the '{directory}' directory might be read-only. Resetting the flags." );

                foreach ( var file in this._fileSystem.EnumerateFiles( directory, "*", SearchOption.AllDirectories ) )
                {
                    this._fileSystem.SetFileAttributes( file, FileAttributes.Normal );
                }

                this._logger.Trace?.Log( $"Retrying to delete '{directory}' directory." );

                this._fileSystem.DeleteDirectory( directory, true );
            }
        }
        catch ( Exception e )
        {
            this._logger.Warning?.Log( $"Cannot delete '{directory}': {e.Message}" );

            var cleanUpFilePath = Path.Combine( directory, _cleanUpFileName );

            try
            {
                // Deleting the directory failed, we set it to be deleted next time.
                // We avoid moving the directory again, because that would lead to a too long path after multiple retries
                // and another move is not necessary anyway.
                // Writing the cleanup file also solves the problem where some content of the directory fails to get deleted,
                // but the cleanup file gets deleted before the failure.

                var cleanUpFile = new CleanUpFile() { Strategy = CleanUpStrategy.AlwaysNoMove };
                this._fileSystem.WriteAllText( cleanUpFilePath, JsonConvert.SerializeObject( cleanUpFile ) );
            }
            catch ( Exception e2 )
            {
                this._logger.Warning?.Log( $"Failed to write '{cleanUpFilePath}' after failed deletion of '{directory}': {e2.Message}" );
            }
        }
    }

    private bool TryMoveDirectory( string sourcePath, [NotNullWhen( true )] out string? targetPath )
    {
        this._logger.Trace?.Log( $"Moving '{sourcePath}'." );

        for ( var i = 0; i < 100; i++ )
        {
            targetPath = sourcePath + i;

            if ( !this._fileSystem.DirectoryExists( targetPath ) )
            {
                try
                {
                    this._fileSystem.MoveDirectory( sourcePath, targetPath );
                }
                catch ( Exception e )
                {
                    this._logger.Warning?.Log( $"Cannot move '{sourcePath}' to '{targetPath}': {e.Message}" );

                    return false;
                }

                this._logger.Trace?.Log( $"Directory '{sourcePath}' moved to '{targetPath}'." );

                return true;
            }
        }

        this._logger.Warning?.Log(
            $"Directory '{sourcePath}' could not be moved, this is likely caused by another directory with same name exists in the same location." );

        targetPath = null;

        return false;
    }

    private void DeleteIndividualFiles( string directory, TimeSpan age )
    {
        var threshold = this._time.UtcNow.Add( -age );

        var preventDirectoryDeletion = false;

        foreach ( var file in this._fileSystem.GetFiles( directory ) )
        {
            var isCleanupFile = Path.GetFileName( file ) == _cleanUpFileName;
            var lastWriteTime = this._fileSystem.GetFileLastWriteTime( file );
            var lastWriteTimeUtc = lastWriteTime.ToUniversalTime();

            if ( lastWriteTimeUtc < threshold )
            {
                if ( isCleanupFile )
                {
                    this._logger.Trace?.Log( $"Not deleting '{file}'." );

                    continue;
                }

                try
                {
                    this._logger.Trace?.Log( $"Deleting '{file}'. It has been last written more than {age} ago at {lastWriteTime:s}." );
                    this._fileSystem.DeleteFile( file );
                }
                catch ( Exception e )
                {
                    this._logger.Warning?.Log( $"Cannot delete '{file}': {e.Message}" );
                    preventDirectoryDeletion = true;
                }
            }
            else
            {
                if ( isCleanupFile )
                {
                    this._logger.Trace?.Log( $"Preventing deletion of the '{directory}'. It has been last written less than {age} ago at {lastWriteTime:s}." );
                    preventDirectoryDeletion = true;
                }
                else
                {
                    this._logger.Trace?.Log( $"Not deleting '{file}'. It has been last written less than {age} ago at {lastWriteTime:s}." );
                    preventDirectoryDeletion = true;
                }
            }
        }

        if ( preventDirectoryDeletion )
        {
            this._logger.Trace?.Log( $"The directory '{directory}' will not be deleted." );
        }
        else
        {
            this._logger.Trace?.Log( $"No files remained in the '{directory}' directory. The directory will be deleted." );

            this.DeleteDirectory( directory, true );
        }
    }

    public string GetTempDirectory(
        string directory,
        CleanUpStrategy cleanUpStrategy,
        string? subdirectory = null,
        TempFileVersionScope versionScope = TempFileVersionScope.Default )
    {
        var version = versionScope switch
        {
            TempFileVersionScope.Backstage => this._backstageVersion,
            TempFileVersionScope.Default => this._applicationVersion,
            TempFileVersionScope.None => "",
            _ => throw new ArgumentOutOfRangeException( nameof(versionScope) )
        };

        var directoryFullPath = Path.Combine(
            this._standardDirectories.TempDirectory,
            directory,
            version,
            subdirectory ?? "" );

        var cleanUpFilePath = Path.Combine( directoryFullPath, _cleanUpFileName );

        if ( !this._fileSystem.DirectoryExists( directoryFullPath ) || !this._fileSystem.FileExists( cleanUpFilePath ) )
        {
            using ( MutexHelper.WithGlobalLock( directoryFullPath ) )
            {
                if ( !this._fileSystem.DirectoryExists( directoryFullPath ) || !this._fileSystem.FileExists( cleanUpFilePath ) )
                {
                    this._fileSystem.CreateDirectory( directoryFullPath );

                    if ( !this._fileSystem.FileExists( cleanUpFilePath ) )
                    {
                        var file = new CleanUpFile() { Strategy = cleanUpStrategy };
                        this._fileSystem.WriteAllText( cleanUpFilePath, JsonConvert.SerializeObject( file ) );

                        return directoryFullPath;
                    }
                }
            }
        }

        // Profiling data shows that calling SetFileLastWriteTime can be slow, so we do it in the background.
        this._backgroundTasksService.Enqueue(
            () =>
            {
                if ( cleanUpStrategy == CleanUpStrategy.WhenUnused
                     && this._fileSystem.GetFileLastWriteTime( cleanUpFilePath ).ToUniversalTime() > this._time.UtcNow.AddDays( -1 ) )
                {
                    using ( MutexHelper.WithGlobalLock( cleanUpFilePath ) )
                    {
                        RetryHelper.Retry( () => this._fileSystem.SetFileLastWriteTime( cleanUpFilePath, this._time.UtcNow.ToLocalTime() ) );
                    }
                }
            } );

        return directoryFullPath;
    }
}