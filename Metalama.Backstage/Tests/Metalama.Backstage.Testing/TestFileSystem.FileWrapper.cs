// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;

namespace Metalama.Backstage.Testing;

public partial class TestFileSystem
{
    private class FileWrapper : FileSystemWrapper
    {
        public FileWrapper( TestFileSystem parent ) : base( parent ) { }

        public override bool Exists( string path ) => this.Parent.Mock.File.Exists( path );

        public override void SetCreationTime( string path, DateTime creationTime ) => this.Parent.Mock.File.SetCreationTime( path, creationTime );

        public override void SetLastAccessTime( string path, DateTime lastAccessTime ) => this.Parent.Mock.File.SetLastAccessTime( path, lastAccessTime );

        public override void SetLastWriteTime( string path, DateTime lastWriteTime ) => this.Parent.Mock.File.SetLastWriteTime( path, lastWriteTime );

        public TResult Execute<TResult>( ExecutionKind executionKind, WatcherChangeTypes changeType, string path, Func<IFile, TResult> action, [CallerMemberName] string operation = "" )
            => this.Execute( executionKind, changeType, path, () => action( this.Parent.Mock.File ), operation );

        public void Execute( ExecutionKind executionKind, WatcherChangeTypes changeType, string path, Action<IFile> action, [CallerMemberName] string operation = "" )
            => this.Execute( executionKind, changeType, path, () => action( this.Parent.Mock.File ), operation );
    }
}