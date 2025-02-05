// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;

namespace Metalama.Backstage.Testing;

public partial class TestFileSystem
{
    private class DirectoryWrapper : FileSystemWrapper
    {
        public DirectoryWrapper( TestFileSystem parent ) : base( parent ) { }

        public override bool Exists( string path ) => this.Parent.Mock.Directory.Exists( path );

        public override void SetCreationTime( string path, DateTime creationTime ) => this.Parent.Mock.Directory.SetCreationTime( path, creationTime );

        public override void SetLastAccessTime( string path, DateTime lastAccessTime ) => this.Parent.Mock.Directory.SetLastAccessTime( path, lastAccessTime );

        public override void SetLastWriteTime( string path, DateTime lastWriteTime ) => this.Parent.Mock.Directory.SetLastWriteTime( path, lastWriteTime );

        public TResult Execute<TResult>( ExecutionKind executionKind, WatcherChangeTypes changeType, string path, Func<IDirectory, TResult> action, [CallerMemberName] string operation = "" )
            => this.Execute( executionKind, changeType, path, () => action( this.Parent.Mock.Directory ), operation );

        public void Execute( ExecutionKind executionKind, WatcherChangeTypes changeType, string path, Action<IDirectory> action, [CallerMemberName] string operation = "" )
            => this.Execute( executionKind, changeType, path, () => action( this.Parent.Mock.Directory ), operation );
    }
}