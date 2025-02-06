// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Metalama.Patterns.Caching.Tests.RedisServer;

internal sealed class TemporaryFile : IDisposable
{
    // ReSharper disable once StringLiteralTypo
    [DllImport( "libc", SetLastError = true )]
#pragma warning disable SA1300
    private static extern int chmod( string pathname, int mode );
#pragma warning restore SA1300

    private bool _disposed;

    public FileInfo FileInfo { get; }

    // ReSharper disable once MemberCanBePrivate.Global
    public TemporaryFile( string prefix = "", string extension = "tmp" )
    {
        this.FileInfo = new FileInfo(
            Path.Combine(
                Path.GetTempPath(),
                string.IsNullOrEmpty( extension )
                    ? prefix + Guid.NewGuid().ToString( "N" )
                    : prefix + Guid.NewGuid().ToString( "N" ) + "." + extension ) );
    }

    public TemporaryFile( Stream stream, string prefix = "", string extension = "tmp" ) : this( prefix, extension )
    {
        using ( stream )
        using ( var destination = this.FileInfo.OpenWrite() )
        {
            stream.CopyTo( destination );
        }

        if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
        {
            chmod( this.FileInfo.FullName, Convert.ToInt32( "777", 8 ) );
        }
    }

    private void DisposeImpl()
    {
        if ( this._disposed )
        {
            return;
        }

        try
        {
            this.FileInfo.Delete();
        }
        catch ( Exception ex )
        {
            Trace.WriteLine( ex );
        }

        this._disposed = true;
    }

    ~TemporaryFile()
    {
        this.DisposeImpl();
    }

    public void Dispose()
    {
        this.DisposeImpl();
        GC.SuppressFinalize( this );
    }

    // ReSharper disable once UnusedMember.Global
    public void CopyTo( Stream result )
    {
        using ( var stream = this.FileInfo.OpenRead() )
        {
            stream.CopyTo( result );
        }

        if ( result.CanSeek )
        {
            result.Seek( 0, SeekOrigin.Begin );
        }
    }
}