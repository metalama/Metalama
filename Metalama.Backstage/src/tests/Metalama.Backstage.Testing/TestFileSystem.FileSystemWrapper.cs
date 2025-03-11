// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Metalama.Backstage.Testing;

public partial class TestFileSystem
{
    private abstract class FileSystemWrapper
    {
        protected TestFileSystem Parent { get; }

        public FileSystemWrapper( TestFileSystem parent )
        {
            this.Parent = parent;
        }

        public abstract bool Exists( string path );

        public abstract void SetCreationTime( string path, DateTime creationTime );

        public abstract void SetLastAccessTime( string path, DateTime lastAccessTime );

        public abstract void SetLastWriteTime( string path, DateTime lastWriteTime );

        protected TResult Execute<TResult>( ExecutionKind executionKind, WatcherChangeTypes changeType, string path, Func<TResult> action, string operation )
        {
            lock ( this.Parent.Mock )
            {
                if ( executionKind == ExecutionKind.Read || executionKind == ExecutionKind.Write )
                {
                    this.Parent.WaitAndThrowIfBlocked( path, true, operation );
                }
                
                this.Parent.RaiseEvent( path, operation );

                var accessTime = this.Parent._time.UtcNow;

                var isCreated = executionKind == ExecutionKind.Write && !this.Exists( path );
                var result = action();

                if ( isCreated )
                {
                    this.SetCreationTime( path, accessTime );
                }

                if ( executionKind == ExecutionKind.Read || executionKind == ExecutionKind.Write )
                {
                    this.SetLastAccessTime( path, accessTime );
                }

                if ( executionKind == ExecutionKind.Write )
                {
                    this.SetLastWriteTime( path, accessTime );
                }

                if ( changeType != 0 )
                {
                    var directoryName = Path.GetDirectoryName( path );

                    if ( directoryName != null && this.Parent._changeWatchers.TryGetValue( directoryName, out var watchers ) )
                    {
                        foreach ( var watcher in watchers )
                        {
#pragma warning disable CA1307
                            var isMatch = path.EndsWith( watcher.Key.Filter.Replace( "*", "" ), StringComparison.OrdinalIgnoreCase );
#pragma warning restore CA1307

                            if ( isMatch )
                            {
                                Task.Run( () => watcher.Value.Invoke( new FileSystemEventArgs( changeType, directoryName, Path.GetFileName( path ) ) ) );
                            }
                        }
                    }
                }

                return result;
            }
        }

        protected void Execute( ExecutionKind executionKind, WatcherChangeTypes changeType, string path, Action action, string operation )
            => _ = this.Execute<object?>(
                executionKind,
                changeType,
                path,
                () =>
                {
                    action();

                    return null;
                },
                operation );
    }
}