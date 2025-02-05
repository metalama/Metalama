// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using Infrastructure_IFileSystem = Metalama.Backstage.Infrastructure.IFileSystem;

namespace Metalama.Backstage.Testing
{
    // Resharper disable UnusedMember.Global

    public partial class TestFileSystem : Infrastructure_IFileSystem
    {
        private enum ExecutionKind
        {
            Manage,
            Read,
            Write
        }

        private readonly ConcurrentDictionary<string, (ManualResetEventSlim Callee, ManualResetEventSlim Caller)> _blockedReads =
            new();

        private readonly ConcurrentDictionary<string, (ManualResetEventSlim Callee, ManualResetEventSlim Caller)> _blockedWrites =
            new();
        
        private readonly ConcurrentDictionary<string, Action> _events = new();

        private readonly List<string> _failedAccesses = [];

        private readonly IDateTimeProvider _time;

        private readonly DirectoryWrapper _directory;

        private readonly FileWrapper _file;

        public MockFileSystem Mock { get; private set; } = new();

        public IReadOnlyList<string> FailedFileAccesses => this._failedAccesses;

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<WatcherHandle, Action<FileSystemEventArgs>>> _changeWatchers = new();

        public TestFileSystem( IServiceProvider serviceProvider )
        {
            this._time = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
            this._directory = new DirectoryWrapper( this );
            this._file = new FileWrapper( this );
        }

        public void Reset()
        {
            this._changeWatchers.Clear();
            this._failedAccesses.Clear();
            this._blockedReads.Clear();
            this._blockedWrites.Clear();
            this.Mock = new MockFileSystem();
        }

        public ManualResetEventSlim BlockRead( string path )
        {
            ManualResetEventSlim readEvent = new();

            if ( !this._blockedReads.TryAdd( path, (new ManualResetEventSlim(), readEvent) ) )
            {
                throw new InvalidOperationException();
            }

            return readEvent;
        }

        public ManualResetEventSlim BlockWrite( string path )
        {
            ManualResetEventSlim writeEvent = new();

            if ( !this._blockedWrites.TryAdd( path, (new ManualResetEventSlim(), writeEvent) ) )
            {
                throw new InvalidOperationException();
            }

            return writeEvent;
        }

        public void Unblock( string path )
        {
            void UnblockEvents( ConcurrentDictionary<string, (ManualResetEventSlim Callee, ManualResetEventSlim Caller)> blockingEvents )
            {
                if ( blockingEvents.TryGetValue( path, out var events ) )
                {
                    events.Callee.Set();

                    if ( !blockingEvents.TryRemove( path, out _ ) )
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            UnblockEvents( this._blockedReads );
            UnblockEvents( this._blockedWrites );
        }

        private void WaitAndThrowIfBlocked( string path, bool write, string operation )
        {
            var blockedFiles = write ? this._blockedWrites : this._blockedReads;

            if ( blockedFiles.TryGetValue( path, out var events ) )
            {
                events.Caller.Set();
                events.Callee.Wait();
                this._failedAccesses.Add( $"{operation}({path})" );

                throw new IOException( $"{operation} failed. File '{path}' in use." );
            }
        }

        private static string GetOperationKey( string operation, string path ) => $"{operation}({path})";
        
        public void SetEvent( string operation, string path, Action action )
        {
            var key = GetOperationKey( operation, path );
            
            if ( !this._events.TryAdd( key, action ) )
            {
                throw new InvalidOperationException();
            }
        }

        public void ResetEvent( string operation, string path )
        {
            var key = GetOperationKey( operation, path );
            
            if ( !this._events.TryRemove( key, out _ ) )
            {
                throw new InvalidOperationException();
            }
        }

        private void RaiseEvent( string path, string operation )
        {
            var key = GetOperationKey( operation, path );
            
            if ( this._events.TryGetValue( key, out var action ) )
            {
                action();
            }
        }

        public string SynchronizationPrefix { get; } = $"{Guid.NewGuid()}_";

        public DateTime GetFileLastWriteTime( string path ) => this._file.Execute( ExecutionKind.Manage, 0, path, f => f.GetLastWriteTime( path ) );

        public void SetFileLastWriteTime( string path, DateTime lastWriteTime )
            => this._file.Execute(
                ExecutionKind.Manage,
                WatcherChangeTypes.Changed,
                path,
                f =>
                {
                    f.SetLastAccessTime( path, lastWriteTime );
                    f.SetLastWriteTime( path, lastWriteTime );
                } );

        public DateTime GetDirectoryLastWriteTime( string path ) => this._directory.Execute( ExecutionKind.Manage, 0, path, d => d.GetLastWriteTime( path ) );

        public void SetDirectoryLastWriteTime( string path, DateTime lastWriteTime )
            => this._directory.Execute(
                ExecutionKind.Manage,
                WatcherChangeTypes.Changed,
                path,
                d =>
                {
                    d.SetLastAccessTime( path, lastWriteTime );
                    d.SetLastWriteTime( path, lastWriteTime );
                } );

        public bool FileExists( [NotNullWhen( true )] string? path ) => this._file.Execute( ExecutionKind.Manage, 0, path!, f => f.Exists( path ) );

        public FileAttributes GetFileAttributes( string path ) => this._file.Execute( ExecutionKind.Manage, 0, path, f => f.GetAttributes( path ) );

        public void SetFileAttributes( string path, FileAttributes fileAttributes )
            => this._file.Execute( ExecutionKind.Manage, WatcherChangeTypes.Changed, path, f => f.SetAttributes( path, fileAttributes ) );

        public bool DirectoryExists( [NotNullWhen( true )] string? path ) => this._directory.Execute( ExecutionKind.Manage, 0, path!, d => d.Exists( path ) );

        // We use GetFiles instead of EnumerateFiles because the EnumerateFiles method doesn't behave as expected.
        public IEnumerable<string> EnumerateFiles( string path ) => this._directory.Execute( ExecutionKind.Manage, 0, path, d => d.GetFiles( path ) );

        // We use GetFiles instead of EnumerateFiles because the EnumerateFiles method doesn't behave as expected.
        public IEnumerable<string> EnumerateFiles( string path, string searchPattern )
            => this._directory.Execute( ExecutionKind.Manage, 0, path, d => d.GetFiles( path, searchPattern ) );

        // We use GetFiles instead of EnumerateFiles because the EnumerateFiles method doesn't behave as expected.
        public IEnumerable<string> EnumerateFiles( string path, string searchPattern, SearchOption searchOption )
            => this._directory.Execute( ExecutionKind.Manage, 0, path, d => d.GetFiles( path, searchPattern, searchOption ) );

        public string[] GetFiles( string path ) => this._directory.Execute( ExecutionKind.Manage, 0, path, d => d.GetFiles( path ) );

        public string[] GetFiles( string path, string searchPattern )
            => this._directory.Execute( ExecutionKind.Manage, 0, path, d => d.GetFiles( path, searchPattern ) );

        public string[] GetFiles( string path, string searchPattern, SearchOption searchOption )
            => this._directory.Execute( ExecutionKind.Manage, 0, path, d => d.GetFiles( path, searchPattern, searchOption ) );

        // We use GetDirectories instead of EnumerateDirectories because the EnumerateDirectories method doesn't behave as expected.
        public IEnumerable<string> EnumerateDirectories( string path )
            => this._directory.Execute( ExecutionKind.Manage, 0, path, d => d.GetDirectories( path ) );

        // We use GetDirectories instead of EnumerateDirectories because the EnumerateDirectories method doesn't behave as expected.
        public IEnumerable<string> EnumerateDirectories( string path, string searchPattern )
            => this._directory.Execute( ExecutionKind.Manage, 0, path, d => d.GetDirectories( path, searchPattern ) );

        // We use GetDirectories instead of EnumerateDirectories because the EnumerateDirectories method doesn't behave as expected.
        public IEnumerable<string> EnumerateDirectories( string path, string searchPattern, SearchOption searchOption )
            => this._directory.Execute( ExecutionKind.Manage, 0, path, d => d.GetDirectories( path, searchPattern, searchOption ) );

        // This method helps to handle cases where the mock returns trailing separator, but BCL does not.
        private static string[] RemoveTrailingSeparators( string[] paths ) => paths.Select( p => p.TrimEnd( Path.DirectorySeparatorChar ) ).ToArray();

        public string[] GetDirectories( string path )
            => this._directory.Execute( ExecutionKind.Manage, 0, path, d => RemoveTrailingSeparators( d.GetDirectories( path ) ) );

        public string[] GetDirectories( string path, string searchPattern )
            => this._directory.Execute( ExecutionKind.Manage, 0, path, d => RemoveTrailingSeparators( d.GetDirectories( path, searchPattern ) ) );

        public string[] GetDirectories( string path, string searchPattern, SearchOption searchOption )
            => this._directory.Execute( ExecutionKind.Manage, 0, path, d => RemoveTrailingSeparators( d.GetDirectories( path, searchPattern, searchOption ) ) );

        public Stream CreateFile( string path ) => this._file.Execute( ExecutionKind.Write, WatcherChangeTypes.Created, path, f => f.Create( path ) );

        public Stream CreateFile( string path, int bufferSize )
            => this._file.Execute( ExecutionKind.Write, WatcherChangeTypes.Created, path, f => f.Create( path, bufferSize ) );

        public Stream CreateFile( string path, int bufferSize, FileOptions options )
            => this._file.Execute( ExecutionKind.Write, WatcherChangeTypes.Created, path, f => f.Create( path, bufferSize, options ) );
        
        public StreamWriter CreateTextFile( string path )
            => this._file.Execute( ExecutionKind.Write, WatcherChangeTypes.Created, path, f => f.CreateText( path ) );

        public string GetTempFileName()
        {
            var path = this.Mock.Path.GetTempFileName();

            // We don't know the path beforehand, so we set the last write time in a separate dummy step.
            return this._file.Execute( ExecutionKind.Write, WatcherChangeTypes.Created, path, _ => path );
        }

        public void CreateDirectory( string path )
            => this._directory.Execute( ExecutionKind.Write, WatcherChangeTypes.Created, path, d => d.CreateDirectory( path ) );

        public Stream Open( string path, FileMode mode )
            => this._file.Execute( ExecutionKind.Write, WatcherChangeTypes.Changed, path, f => f.Open( path, mode ) );

        public Stream Open( string path, FileMode mode, FileAccess access )
            => this._file.Execute(
                access == FileAccess.Read ? ExecutionKind.Read : ExecutionKind.Write,
                access == FileAccess.Read ? 0 : WatcherChangeTypes.Changed,
                path,
                f => f.Open( path, mode, access ) );

        public Stream Open( string path, FileMode mode, FileAccess access, FileShare share )
            => this._file.Execute(
                access == FileAccess.Read ? ExecutionKind.Read : ExecutionKind.Write,
                access == FileAccess.Read ? 0 : WatcherChangeTypes.Changed,
                path,
                f => f.Open( path, mode, access, share ) );

        private WatcherChangeTypes GetWriteChangeKind( string path ) => this.FileExists( path ) ? WatcherChangeTypes.Changed : WatcherChangeTypes.Created;

        // TODO: Support for bufferSize and options, which are not needed in tests at the moment.
        public Stream Open( string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options )
            => this.Open( path, mode, access, share );

        public Stream OpenRead( string path ) => this._file.Execute( ExecutionKind.Read, 0, path, f => f.OpenRead( path ) );

        public Stream OpenWrite( string path ) => this._file.Execute( ExecutionKind.Write, this.GetWriteChangeKind( path ), path, f => f.OpenWrite( path ) );

        public byte[] ReadAllBytes( string path ) => this._file.Execute( ExecutionKind.Read, 0, path, f => f.ReadAllBytes( path ) );

        public void WriteAllBytes( string path, byte[] bytes )
            => this._file.Execute( ExecutionKind.Write, this.GetWriteChangeKind( path ), path, f => f.WriteAllBytes( path, bytes ) );

        public string ReadAllText( string path ) => this._file.Execute( ExecutionKind.Read, 0, path, f => f.ReadAllText( path ) );

        public void WriteAllText( string path, string? content )
            => this._file.Execute( ExecutionKind.Write, this.GetWriteChangeKind( path ), path, f => f.WriteAllText( path, content ) );
        
        public void WriteAllText( string path, string? contents, Encoding encoding )
            => this._file.Execute( ExecutionKind.Write, this.GetWriteChangeKind( path ), path, f => f.WriteAllText( path, contents, encoding ) );

        public string[] ReadAllLines( string path ) => this._file.Execute( ExecutionKind.Read, 0, path, f => f.ReadAllLines( path ) );

        public void WriteAllLines( string path, string[] contents )
            => this._file.Execute( ExecutionKind.Write, this.GetWriteChangeKind( path ), path, f => f.WriteAllLines( path, contents ) );

        public void WriteAllLines( string path, IEnumerable<string> contents )
            => this._file.Execute( ExecutionKind.Write, this.GetWriteChangeKind( path ), path, f => f.WriteAllLines( path, contents ) );

        public void AppendAllLines( string path, IEnumerable<string> contents )
            => this._file.Execute( ExecutionKind.Write, this.GetWriteChangeKind( path ), path, f => f.AppendAllLines( path, contents ) );
        
        public void AppendAllLines( string path, IEnumerable<string> contents, Encoding encoding )
            => this._file.Execute( ExecutionKind.Write, this.GetWriteChangeKind( path ), path, f => f.AppendAllLines( path, contents, encoding ) );
        
        public void AppendAllText( string path, string? contents )
            => this._file.Execute( ExecutionKind.Write, this.GetWriteChangeKind( path ), path, f => f.AppendAllText( path, contents ) );
        
        public void AppendAllText( string path, string? contents, Encoding encoding )
            => this._file.Execute( ExecutionKind.Write, this.GetWriteChangeKind( path ), path, f => f.AppendAllText( path, contents, encoding ) );

        public void MoveFile( string sourceFileName, string destFileName )
        {
            this._file.Execute( ExecutionKind.Manage, WatcherChangeTypes.Created, destFileName, f => f.Move( sourceFileName, destFileName ) );

            // This is to raise the change event.
            this._file.Execute( ExecutionKind.Manage, WatcherChangeTypes.Deleted, sourceFileName, _ => { } );
        }

        public void DeleteFile( string path ) => this._file.Execute( ExecutionKind.Manage, WatcherChangeTypes.Deleted, path, f => f.Delete( path ) );

        public void MoveDirectory( string sourceDirName, string destDirName )
        {
            this._directory.Execute( ExecutionKind.Manage, WatcherChangeTypes.Created, destDirName, d => d.Move( sourceDirName, destDirName ) );

            // This is to raise the change event.
            this._file.Execute( ExecutionKind.Manage, WatcherChangeTypes.Deleted, sourceDirName, _ => { } );
        }

        public void DeleteDirectory( string path, bool recursive )
            => this._directory.Execute( ExecutionKind.Manage, WatcherChangeTypes.Deleted, path, d => d.Delete( path, recursive ) );

        public bool IsDirectoryEmpty( string path ) => this._directory.Execute( ExecutionKind.Read, 0, path, d => !d.EnumerateFileSystemEntries( path ).Any() );

        public IDisposable WatchChanges( string directory, string filter, Action<FileSystemEventArgs> callback )
        {
            var subDictionary = this._changeWatchers.GetOrAdd( directory, _ => new ConcurrentDictionary<WatcherHandle, Action<FileSystemEventArgs>>() );
            var handle = new WatcherHandle( this, directory, filter );
            subDictionary.TryAdd( handle, callback );

            return handle;
        }

        private class WatcherHandle : IDisposable
        {
            private readonly TestFileSystem _fileSystem;
            private readonly string _directory;

            public string Filter { get; }

            public WatcherHandle( TestFileSystem fileSystem, string directory, string filter )
            {
                this._fileSystem = fileSystem;
                this._directory = directory;
                this.Filter = filter;
            }

            public void Dispose()
            {
                if ( this._fileSystem._changeWatchers.TryGetValue( this._directory, out var subDictionary ) )
                {
                    subDictionary.TryRemove( this, out _ );
                }
            }
        }

        public void ExtractZipArchiveToDirectory( ZipArchive sourceZipArchive, string destinationDirectoryPath )
            => TestFileSystemZipUtilities.ExtractToDirectory( this, sourceZipArchive, destinationDirectoryPath );
    }
}