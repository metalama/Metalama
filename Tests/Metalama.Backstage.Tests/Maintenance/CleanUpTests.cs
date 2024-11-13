// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Testing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Maintenance;

public class CleanUpTests : TestsBase
{
    private readonly int _subdirectoriesPerCacheDirectory = 5;
    private readonly IStandardDirectories _standardDirectories;

    private readonly ImmutableDictionary<string, CleanUpStrategy> _cleanUpStrategyByDirectory = new Dictionary<string, CleanUpStrategy>()
    {
        { "AssemblyLocator", CleanUpStrategy.WhenUnused },
        { "CompileTime", CleanUpStrategy.WhenUnused },
        { "CompileTimeTroubleshooting", CleanUpStrategy.Always },
        { "CrashReports", CleanUpStrategy.FileOneMonthAfterCreation },
        { "Extract", CleanUpStrategy.None },
        { "Logs", CleanUpStrategy.Always },
        { "Preview", CleanUpStrategy.WhenUnused },
        { "Tests", CleanUpStrategy.WhenUnused }
    }.ToImmutableDictionary();

    public CleanUpTests( ITestOutputHelper logger ) : base( logger )
    {
        this._standardDirectories = this.ServiceProvider.GetRequiredBackstageService<IStandardDirectories>();
        this.SetupTempDirectory();
    }

    protected override void ConfigureServices( ServiceProviderBuilder services )
    {
        services
            .AddSingleton<IApplicationInfoProvider>( new ApplicationInfoProvider( new TestApplicationInfo() ) );
    }

    private void SetupTempDirectory()
    {
        // Create main directories.
        this.FileSystem.CreateDirectory( this._standardDirectories.ApplicationDataDirectory );
        var tempDirectoryPath = this._standardDirectories.TempDirectory;
        this.FileSystem.CreateDirectory( tempDirectoryPath );

        // Populate the cache directories with cleanup files according to strategies.
        foreach ( var pair in this._cleanUpStrategyByDirectory )
        {
            var cacheDirectoryPath = Path.Combine( tempDirectoryPath, pair.Key );

            this.FileSystem.CreateDirectory( cacheDirectoryPath );
            this.Logger.WriteLine( $"Populating '{cacheDirectoryPath}'." );

            for ( var i = 0; i < this._subdirectoriesPerCacheDirectory; i++ )
            {
                var subdirectoryPath = Path.Combine( cacheDirectoryPath, $"0.1.{42 + i}-test" );
                this.FileSystem.CreateDirectory( subdirectoryPath );

                var subdirectoryCleanUpFilePath = Path.Combine( subdirectoryPath, "cleanup.json" );
                var cacheDirectoryStrategy = pair.Value;
                var cleanUpFile = new CleanUpFile { Strategy = cacheDirectoryStrategy };
                var cleanUpStrategy = JsonConvert.SerializeObject( cleanUpFile );
                this.FileSystem.WriteAllText( subdirectoryCleanUpFilePath, cleanUpStrategy );
            }
        }

        // Create a deeper directory structure and populate it with cleanup files at the bottom level.
        var rootDirectory = Path.Combine( this._standardDirectories.TempDirectory, "DeepDirectory" );
        this.Logger.WriteLine( $"Populating '{rootDirectory}' with deeper structure of directories." );

        for ( var i = 0; i < 5; i++ )
        {
            var currentDirectory = Path.Combine( rootDirectory, $"subdirectory_{i}" );

            for ( var j = 0; j < 4; j++ )
            {
                currentDirectory = Path.Combine( currentDirectory, $"subdirectory{i}{j}" );
            }

            this.FileSystem.CreateDirectory( currentDirectory );
            var cleanUpFile = new CleanUpFile { Strategy = CleanUpStrategy.Always };
            var cleanUpStrategy = JsonConvert.SerializeObject( cleanUpFile );
            this.FileSystem.WriteAllText( Path.Combine( currentDirectory, "cleanup.json" ), cleanUpStrategy );
        }
    }

    /// <summary>
    /// Looks for every cleanup.json file recursively in specified <paramref name="path"/> and sets specified <paramref name="lastWriteTime"/> of the file.
    /// </summary>
    /// <param name="path">Path from where you look for the clean-up files.</param>
    /// <param name="lastWriteTime"></param>
    private void SetLastWriteTimeOfCleanUpFile( string path, DateTime lastWriteTime )
    {
        var cleanUpFilePaths = this.FileSystem.EnumerateFiles( path, "cleanup.json", SearchOption.AllDirectories );

        foreach ( var cleanUpFile in cleanUpFilePaths )
        {
            this.FileSystem.SetFileLastWriteTime( cleanUpFile, lastWriteTime );
        }
    }

    private void AssertOnlyCleanupFilesRemained( string directory )
    {
        var file = Assert.Single( this.FileSystem.GetFiles( directory, "*.*", SearchOption.AllDirectories ).Select( Path.GetFileName ).Distinct() );
        Assert.Equal( "cleanup.json", file );
    }

    private void TestIndividualFilesAreDeleted( string directory, CleanUpStrategy strategy, TempFileManager tempFileManager )
    {
        this.AssertOnlyCleanupFilesRemained( directory );

        var directoryDeletionDelay = strategy switch
        {
            CleanUpStrategy.Always => TimeSpan.FromHours( 4 ),
            CleanUpStrategy.FileOneMonthAfterCreation => TimeSpan.FromDays( 30 ),
            _ => throw new InvalidOperationException( $"The strategy '{strategy}' is invalid for '{nameof(this.TestIndividualFilesAreDeleted)}' method." )
        };
        
        this.Time.AddTime( directoryDeletionDelay );
        tempFileManager.CleanTempDirectories( true );
        Assert.False( this.FileSystem.DirectoryExists( directory ) );
    }

    [Fact]
    public void Clean_All()
    {
        // Clean-up command should clean everything.
        var tempFileManager = new TempFileManager( this.ServiceProvider );
        tempFileManager.CleanTempDirectories( true, true );

        // Assert every cache directory is deleted.
        Assert.False( this.FileSystem.DirectoryExists( this._standardDirectories.TempDirectory ) );
    }

    [Fact]
    public void Clean_NoOutdatedFiles()
    {
        // Make all cleanup.json files with recent last write time.
        this.SetLastWriteTimeOfCleanUpFile( this._standardDirectories.TempDirectory, DateTime.Now );

        // Clean-up command should skip directories with WhenUnused, as those files are recently created.
        var tempFileManager = new TempFileManager( this.ServiceProvider );
        tempFileManager.CleanTempDirectories( true );

        foreach ( var cacheDirectory in this._cleanUpStrategyByDirectory )
        {
            var cacheDirectoryPath = Path.Combine( this._standardDirectories.TempDirectory, cacheDirectory.Key );

            switch ( cacheDirectory.Value )
            {
                case CleanUpStrategy.WhenUnused:
                    // Assert there are still uncleaned directories as they have been used in past 7 days.
                    Assert.True( this.FileSystem.DirectoryExists( cacheDirectoryPath ) );
                    Assert.NotEmpty( this.FileSystem.EnumerateDirectories( cacheDirectoryPath ) );

                    break;

                case CleanUpStrategy.Always:
                case CleanUpStrategy.FileOneMonthAfterCreation:
                    // Assert always cleaned directories are empty.
                    // Assert the same for FileOneMonthAfterCreation - this strategy is tested by IndividualFilesGetDeletedAfterOneMonth test.
                    this.AssertOnlyCleanupFilesRemained( cacheDirectoryPath );

                    break;

                default:
                    // No strategy directories should be kept.
                    Assert.NotEmpty( this.FileSystem.EnumerateDirectories( cacheDirectoryPath ) );

                    break;
            }
        }
    }

    [Fact]
    public void Clean_OutdatedFiles()
    {
        // Make all cleanup files outdated.
        this.SetLastWriteTimeOfCleanUpFile( this._standardDirectories.TempDirectory, DateTime.Now.AddDays( -10 ) );

        // Clean-up command should clean directories with WhenUnused, as those files are outdated.
        var tempFileManager = new TempFileManager( this.ServiceProvider );
        tempFileManager.CleanTempDirectories( true );

        foreach ( var cacheDirectory in this._cleanUpStrategyByDirectory )
        {
            var cacheDirectoryPath = Path.Combine( this._standardDirectories.TempDirectory, cacheDirectory.Key );

            switch ( cacheDirectory.Value )
            {
                case CleanUpStrategy.WhenUnused:
                    Assert.False( this.FileSystem.DirectoryExists( cacheDirectoryPath ) );

                    break;
                    
                case CleanUpStrategy.Always:
                case CleanUpStrategy.FileOneMonthAfterCreation:
                    // Assert always cleaned directories and outdated directories are empty.
                    // Assert the same for FileOneMonthAfterCreation - this strategy is tested by IndividualFilesGetDeletedAfterOneMonth test.                    
                    if ( this.FileSystem.DirectoryExists( cacheDirectoryPath ) )
                    {
                        this.AssertOnlyCleanupFilesRemained( cacheDirectoryPath );
                    }

                    break;

                // No strategy directories should be kept.
                default:
                    Assert.NotEmpty( this.FileSystem.EnumerateDirectories( cacheDirectoryPath ) );

                    break;
            }
        }
    }

    [Fact]
    public void Clean_Scheduled()
    {
        // Create cleanup.json and update its LastCleanUp property to a week ago.
        var configurationManager = this.ServiceProvider.GetRequiredBackstageService<IConfigurationManager>();
        configurationManager.Update<CleanUpConfiguration>( c => c with { LastCleanUpTime = DateTime.Now.AddDays( -7 ) } );

        // Clean-up command should begin cleaning as the last clean-up was more than a day ago.
        var tempFileManager = new TempFileManager( this.ServiceProvider );
        tempFileManager.CleanTempDirectories();

        foreach ( var cacheDirectory in this._cleanUpStrategyByDirectory )
        {
            var cacheDirectoryPath = Path.Combine( this._standardDirectories.TempDirectory, cacheDirectory.Key );

            switch ( cacheDirectory.Value )
            {
                case CleanUpStrategy.WhenUnused:
                    // Assert directories no older than 7 days are still not empty.
                    Assert.NotEmpty( this.FileSystem.EnumerateDirectories( cacheDirectoryPath ) );

                    break;

                case CleanUpStrategy.Always:
                case CleanUpStrategy.FileOneMonthAfterCreation:
                    // Assert always cleaned directories are empty.
                    // Assert the same for FileOneMonthAfterCreation - this strategy is tested by IndividualFilesGetDeletedAfterOneMonth test.
                    if ( this.FileSystem.DirectoryExists( cacheDirectoryPath ) )
                    {
                        this.AssertOnlyCleanupFilesRemained( cacheDirectoryPath );
                    }

                    break;

                default:
                    // No strategy directories should be kept.
                    Assert.NotEmpty( this.FileSystem.EnumerateDirectories( cacheDirectoryPath ) );

                    break;
            }
        }
    }

    [Fact]
    public void Clean_AttemptedBeforeSchedule()
    {
        // Create cleanup.json with LastCleanUp property that will not start clean.
        var configurationManager = this.ServiceProvider.GetRequiredBackstageService<IConfigurationManager>();
        configurationManager.Update<CleanUpConfiguration>( c => c with { LastCleanUpTime = DateTime.Now } );

        // Clean-up command should skip cleaning as it was attempted too early.
        var tempFileManager = new TempFileManager( this.ServiceProvider );
        tempFileManager.CleanTempDirectories();

        foreach ( var cacheDirectory in this._cleanUpStrategyByDirectory )
        {
            var cacheDirectoryPath = Path.Combine( this._standardDirectories.TempDirectory, cacheDirectory.Key );

            // Directories should not be cleaned as there last clean-up was less than a day ago.
            var directories = this.FileSystem.EnumerateDirectories( cacheDirectoryPath ).ToList();
            Assert.NotEmpty( directories );
        }
    }

    [Fact]
    public void Clean_DeepDirectoryStructure()
    {
        // Clean-up command should be able to clean the deep structured directories.
        var tempFileManager = new TempFileManager( this.ServiceProvider );
        tempFileManager.CleanTempDirectories( true );
        var deepDirectory = Path.Combine( this._standardDirectories.TempDirectory, "DeepDirectory" );

        this.TestIndividualFilesAreDeleted( deepDirectory, CleanUpStrategy.Always, tempFileManager );
    }

    [Fact]
    public void Clean_ReadOnlyFiles()
    {
        // Create a read-only file.
        var directoryPath = Path.Combine( this._standardDirectories.TempDirectory, "CompileTime", "0.1.42-test" );
        var readOnlyFilePath = Path.Combine( directoryPath, "ReadOnlyFile.txt" );
        this.FileSystem.WriteAllText( readOnlyFilePath, "Test" );
        this.FileSystem.SetFileAttributes( readOnlyFilePath, FileAttributes.ReadOnly );

        // Clean-up command should be able to clean the read-only files.
        var tempFileManager = new TempFileManager( this.ServiceProvider );
        tempFileManager.CleanTempDirectories( true, true );

        // Issue #34974: On Windows, the directory with a read-only file is allowed to be moved, but read-only file is disallowed to be deleted.
        // Some files may get deleted before this is hit. We have the CleanUpFileIsNotRemovedOnFailure test for this scenario.
        Assert.False( this.FileSystem.DirectoryExists( $"{directoryPath}0" ) );
    }

    [Fact]
    public void CleanUpFileIsNotRemovedOnFailure()
    {
        var directoryPath = Path.Combine( this._standardDirectories.TempDirectory, "AssemblyLocator", "0.1.42-test" );

        // This file will remain in the directory after the failure.
        this.FileSystem.WriteAllText( Path.Combine( directoryPath, "foo.bar" ), "Foo" );
        this.Time.AddTime( TimeSpan.FromDays( 32 ) );

        var deleteDirectoryOperation = nameof(this.FileSystem.DeleteDirectory);
        var newDirectoryPath = $"{directoryPath}0";

        this.FileSystem.SetEvent(
            deleteDirectoryOperation,
            newDirectoryPath,
            () =>
            {
                // The cleanup.json gets sometimes deleted first when deleting a directory, where some content fails to get deleted.
                // We simulate this by deleting it manually.
                this.FileSystem.DeleteFile( Path.Combine( newDirectoryPath, "cleanup.json" ) );

                // Simulate a failure by throwing an exception. 
                throw new UnauthorizedAccessException();
            } );

        var tempFileManager = new TempFileManager( this.ServiceProvider );
        tempFileManager.CleanTempDirectories( true );

        // The directory should have failed to delete.
        Assert.True( this.FileSystem.DirectoryExists( newDirectoryPath ) );

        // Retry the operation with no failure.
        this.FileSystem.ResetEvent( deleteDirectoryOperation, newDirectoryPath );
        tempFileManager.CleanTempDirectories( true );

        Assert.False( this.FileSystem.DirectoryExists( newDirectoryPath ) );
    }

    [Fact]
    public void IndividualFilesGetDeletedAfterOneMonth()
    {
        var directoryPath = Path.Combine( this._standardDirectories.TempDirectory, "CrashReports", "0.1.42-test" );
        var oldFilePath = Path.Combine( directoryPath, "oldFile.txt" );
        var newFilePath = Path.Combine( directoryPath, "newFile.txt" );
        this.FileSystem.WriteAllText( oldFilePath, "Old" );
        this.Time.AddTime( TimeSpan.FromDays( 32 ) );
        this.FileSystem.WriteAllText( newFilePath, "New" );
        this.Time.AddTime( TimeSpan.FromDays( 29 ) );

        var tempFileManager = new TempFileManager( this.ServiceProvider );
        tempFileManager.CleanTempDirectories( true );

        Assert.False( this.FileSystem.FileExists( oldFilePath ) );
        Assert.True( this.FileSystem.FileExists( newFilePath ) );
    }
    
    [Fact]
    public void AlwaysStrategyCleansUpIndividualFilesAfter4Hours()
    {
        var directoryPath = Path.Combine( this._standardDirectories.TempDirectory, "Logs", "0.1.42-test" );
        var oldFilePath = Path.Combine( directoryPath, "oldFile.txt" );
        var newFilePath = Path.Combine( directoryPath, "newFile.txt" );
        this.FileSystem.WriteAllText( oldFilePath, "Old" );
        this.Time.AddTime( TimeSpan.FromHours( 5 ) );
        this.FileSystem.WriteAllText( newFilePath, "New" );
        this.Time.AddTime( TimeSpan.FromHours( 3 ) );

        var tempFileManager = new TempFileManager( this.ServiceProvider );
        tempFileManager.CleanTempDirectories( true );

        Assert.False( this.FileSystem.FileExists( oldFilePath ) );
        Assert.True( this.FileSystem.FileExists( newFilePath ) );
    }
}