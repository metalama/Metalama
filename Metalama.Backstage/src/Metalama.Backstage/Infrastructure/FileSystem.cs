// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Utilities;
using Metalama.Testing.Hooks;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Metalama.Backstage.Infrastructure
{
    /// <summary>
    /// Provides access to file system using API in <see cref="System.IO" /> namespace.
    /// </summary>
    internal sealed class FileSystem : IFileSystem
    {
        private readonly IServiceProvider? _serviceProvider;

        /// <summary>
        /// The provider of the test synchronization points, which is never registered in production and is therefore
        /// normally <see langword="null"/>.
        /// </summary>
        private readonly ITestSynchronizationProvider? _testSynchronizationProvider;

        private IStandardDirectories? _standardDirectories;

        public FileSystem() { }

        public FileSystem( IServiceProvider serviceProvider )
        {
            this._serviceProvider = serviceProvider;

            // Resolved untyped, because ITestSynchronizationProvider is shared with the layers above and therefore
            // cannot derive from IBackstageService.
            this._testSynchronizationProvider = (ITestSynchronizationProvider?) serviceProvider.GetService( typeof(ITestSynchronizationProvider) );
        }

        /// <summary>
        /// The location of the synchronization point reached by <see cref="WriteAllTextAtomically"/> once the
        /// temporary file holds the new content and the existence of the destination has been established, but
        /// before the destination is substituted.
        /// </summary>
        /// <remarks>
        /// The point is deliberately between the test of the destination and the substitution, because that is the
        /// window in which the two ways the substitution can fail are both reachable: a test pinning a writer here
        /// can create the destination, which invalidates the test that was just made, or hold the destination open,
        /// which prevents it from being replaced.
        /// </remarks>
        internal const string BeforeSubstitutionLocation = "WriteAllTextAtomicallyBeforeSubstitution";

        /// <summary>
        /// Composes the name of a synchronization point, following the <c>{ClassName}.{Location}:{Context}</c>
        /// convention. The context is the path of the destination, so that a test can pin one file without pinning
        /// every other file written by the process.
        /// </summary>
        /// <param name="location">One of the <c>Location</c> constants of this class.</param>
        /// <param name="path">The path of the file being written.</param>
        /// <returns>The name of the synchronization point.</returns>
        internal static string GetSyncPointName( string location, string path )
            => string.Format( CultureInfo.InvariantCulture, "FileSystem.{0}:{1}", location, path );

        /// <inheritdoc />
        public DateTime GetFileLastWriteTime( string path )
        {
            return File.GetLastWriteTime( path );
        }

        /// <inheritdoc />
        public void SetFileLastWriteTime( string path, DateTime lastWriteTime )
        {
            File.SetLastWriteTime( path, lastWriteTime );
        }

        /// <inheritdoc />
        public DateTime GetDirectoryLastWriteTime( string path )
        {
            return Directory.GetLastWriteTime( path );
        }

        /// <inheritdoc />
        public DateTime GetDirectoryCreationTime( string path )
        {
            return Directory.GetCreationTime( path );
        }

        /// <inheritdoc />
        public void SetDirectoryLastWriteTime( string path, DateTime lastWriteTime )
        {
            Directory.SetLastWriteTime( path, lastWriteTime );
        }

        /// <inheritdoc />
        public bool FileExists( [NotNullWhen( true )] string? path )
        {
            return File.Exists( path );
        }

        /// <inheritdoc />
        public FileAttributes GetFileAttributes( string path )
        {
            return File.GetAttributes( path );
        }

        /// <inheritdoc />
        public void SetFileAttributes( string path, FileAttributes fileAttributes )
        {
            File.SetAttributes( path, fileAttributes );
        }

        /// <inheritdoc />
        public bool DirectoryExists( [NotNullWhen( true )] string? path )
        {
            return Directory.Exists( path );
        }

        /// <inheritdoc />
        public string[] GetFiles( string path )
        {
            return Directory.GetFiles( path );
        }

        /// <inheritdoc />
        public string[] GetFiles( string path, string searchPattern )
        {
            return Directory.GetFiles( path, searchPattern );
        }

        /// <inheritdoc />
        public string[] GetFiles( string path, string searchPattern, SearchOption searchOption )
        {
            return Directory.GetFiles( path, searchPattern, searchOption );
        }

        /// <inheritdoc />
        public IEnumerable<string> EnumerateFiles( string path )
        {
            return Directory.EnumerateFiles( path );
        }

        /// <inheritdoc />
        public IEnumerable<string> EnumerateFiles( string path, string searchPattern )
        {
            return Directory.EnumerateFiles( path, searchPattern );
        }

        /// <inheritdoc />
        public IEnumerable<string> EnumerateFiles( string path, string searchPattern, SearchOption searchOption )
        {
            return Directory.EnumerateFiles( path, searchPattern, searchOption );
        }

        /// <inheritdoc />
        public string[] GetDirectories( string path )
        {
            return Directory.GetDirectories( path );
        }

        /// <inheritdoc />
        public string[] GetDirectories( string path, string searchPattern )
        {
            return Directory.GetDirectories( path, searchPattern );
        }

        /// <inheritdoc />
        public string[] GetDirectories( string path, string searchPattern, SearchOption searchOption )
        {
            return Directory.GetDirectories( path, searchPattern, searchOption );
        }

        /// <inheritdoc />
        public IEnumerable<string> EnumerateDirectories( string path )
        {
            return Directory.EnumerateDirectories( path );
        }

        /// <inheritdoc />
        public IEnumerable<string> EnumerateDirectories( string path, string searchPattern )
        {
            return Directory.EnumerateDirectories( path, searchPattern );
        }

        /// <inheritdoc />
        public IEnumerable<string> EnumerateDirectories( string path, string searchPattern, SearchOption searchOption )
        {
            return Directory.EnumerateDirectories( path, searchPattern, searchOption );
        }

        /// <inheritdoc />
        public Stream CreateFile( string path )
        {
            return File.Create( path );
        }

        /// <inheritdoc />
        public Stream CreateFile( string path, int bufferSize )
        {
            return File.Create( path, bufferSize );
        }

        /// <inheritdoc />
        public Stream CreateFile( string path, int bufferSize, FileOptions options )
        {
            return File.Create( path, bufferSize, options );
        }

        /// <inheritdoc />
        public StreamWriter CreateTextFile( string path )
        {
            return File.CreateText( path );
        }

        /// <inheritdoc />
        public string GetTempFileName()
        {
            // When this service has its own service provider (e.g. in the worker process, which never initializes the
            // global BackstageServiceFactory), resolve the temp directory from it instead of relying on the static accessor.
            if ( this._serviceProvider != null )
            {
                this._standardDirectories ??= this._serviceProvider.GetRequiredBackstageService<IStandardDirectories>();

                return MetalamaPathUtilities.GetTempFileName( this._standardDirectories.TempDirectory );
            }

            return MetalamaPathUtilities.GetTempFileName();
        }

        /// <inheritdoc />
        public void CreateDirectory( string path )
        {
            Directory.CreateDirectory( path );
        }

        /// <inheritdoc />
        public Stream Open( string path, FileMode mode )
        {
            return File.Open( path, mode );
        }

        /// <inheritdoc />
        public Stream Open( string path, FileMode mode, FileAccess access )
        {
            return File.Open( path, mode, access );
        }

        /// <inheritdoc />
        public Stream Open( string path, FileMode mode, FileAccess access, FileShare share )
        {
            return File.Open( path, mode, access, share );
        }

        /// <inheritdoc />
        public Stream Open( string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options )
        {
            return new FileStream( path, mode, access, share, bufferSize, options );
        }

        /// <inheritdoc />
        public Stream OpenRead( string path )
        {
            return File.OpenRead( path );
        }

        /// <inheritdoc />
        public Stream OpenWrite( string path )
        {
            return File.OpenWrite( path );
        }

        /// <inheritdoc />
        public byte[] ReadAllBytes( string path )
        {
            return File.ReadAllBytes( path );
        }

        /// <inheritdoc />
        public void WriteAllBytes( string path, byte[] bytes )
        {
            File.WriteAllBytes( path, bytes );
        }

        /// <inheritdoc />
        public string ReadAllText( string path )
        {
            return File.ReadAllText( path );
        }

        /// <inheritdoc />
        public void WriteAllText( string path, string? content )
        {
            File.WriteAllText( path, content );
        }

        /// <inheritdoc />
        public void WriteAllText( string path, string? contents, Encoding encoding )
        {
            File.WriteAllText( path, contents, encoding );
        }

        /// <inheritdoc />
        public void WriteAllTextAtomically( string path, string? content )
            => RetryHelper.Retry(
                () =>
                {
                    // A new temporary file for every attempt: the previous one has been deleted, and two attempts
                    // that were to reuse a name would depend on that deletion having completed.
                    var tempPath = GetTemporarySiblingPath( path );

                    try
                    {
                        File.WriteAllText( tempPath, content );

                        var destinationExists = File.Exists( path );

                        this._testSynchronizationProvider?.SyncPoint( GetSyncPointName( BeforeSubstitutionLocation, path ) );

                        if ( destinationExists )
                        {
                            // File.Replace requires the destination to exist, and preserves its access control list.
                            // It fails while a reader holds the destination open without FileShare.Delete, which is
                            // the race the retry is there to absorb.
                            File.Replace( tempPath, path, destinationBackupFileName: null );
                        }
                        else
                        {
                            // File.Move throws when the destination exists, which is what makes this branch safe: if
                            // the destination appeared between the test above and this call, the next attempt finds
                            // it and substitutes it properly instead of this one failing to create it.
                            File.Move( tempPath, path );
                        }
                    }
                    finally
                    {
                        if ( File.Exists( tempPath ) )
                        {
                            File.Delete( tempPath );
                        }
                    }
                } );

        /// <summary>
        /// Returns the path of a file that does not exist, located in the same directory as <paramref name="path"/>.
        /// </summary>
        /// <remarks>
        /// The name begins with a period, so that the file is hidden on Unix, and ends with <c>.tmp</c>, so that a
        /// caller enumerating the directory by the extension of the destination does not observe it. The identifier
        /// in the middle makes two concurrent writers use two different temporary files.
        /// </remarks>
        private static string GetTemporarySiblingPath( string path )
            => Path.Combine( Path.GetDirectoryName( path ) ?? ".", $".{Path.GetFileName( path )}.{Guid.NewGuid():N}.tmp" );

        /// <inheritdoc />
        public string[] ReadAllLines( string path )
        {
            return File.ReadAllLines( path );
        }

        /// <inheritdoc />
        public void WriteAllLines( string path, string[] contents )
        {
            File.WriteAllLines( path, contents );
        }

        /// <inheritdoc />
        public void WriteAllLines( string path, IEnumerable<string> contents )
        {
            File.WriteAllLines( path, contents );
        }

        /// <inheritdoc />
        public void AppendAllLines( string path, IEnumerable<string> contents )
        {
            File.AppendAllLines( path, contents );
        }

        /// <inheritdoc />
        public void AppendAllLines( string path, IEnumerable<string> contents, Encoding encoding )
        {
            File.AppendAllLines( path, contents, encoding );
        }

        /// <inheritdoc />
        public void AppendAllText( string path, string? contents )
        {
            File.AppendAllText( path, contents );
        }

        /// <inheritdoc />
        public void AppendAllText( string path, string? contents, Encoding encoding )
        {
            File.AppendAllText( path, contents, encoding );
        }

        /// <inheritdoc />
        public void MoveFile( string sourceFileName, string destFileName )
        {
            File.Move( sourceFileName, destFileName );
        }

        /// <inheritdoc />
        public void DeleteFile( string path )
        {
            File.Delete( path );
        }

        /// <inheritdoc />
        public void MoveDirectory( string sourceDirName, string destDirName )
        {
            Directory.Move( sourceDirName, destDirName );
        }

        /// <inheritdoc />
        public void DeleteDirectory( string path, bool recursive )
        {
            Directory.Delete( path, recursive );
        }

        /// <inheritdoc />
        public bool IsDirectoryEmpty( string path ) => !Directory.EnumerateFileSystemEntries( path ).Any();

        /// <inheritdoc />
        public void ExtractZipArchiveToDirectory( ZipArchive sourceZipArchive, string destinationDirectoryPath )
            => sourceZipArchive.ExtractToDirectory( destinationDirectoryPath );

        public IDisposable WatchChanges( string directory, string filter, Action<FileSystemEventArgs> callback )
        {
            var fileSystemWatcher = new FileSystemWatcher( directory, filter );
            fileSystemWatcher.Created += ( _, args ) => callback( args );
            fileSystemWatcher.Changed += ( _, args ) => callback( args );
            fileSystemWatcher.Deleted += ( _, args ) => callback( args );
            fileSystemWatcher.EnableRaisingEvents = true;

            return fileSystemWatcher;
        }
    }
}