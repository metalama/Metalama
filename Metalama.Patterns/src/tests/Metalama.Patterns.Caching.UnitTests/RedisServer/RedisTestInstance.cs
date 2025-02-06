// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Tests.Executables;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Metalama.Patterns.Caching.Tests.RedisServer;

#pragma warning disable CA2201

// ReSharper disable once CommentTypo
// Originally taken from github.com/poulfoged/redis-inside (no strong name in the distributed package).

// ReSharper disable MemberCanBeInternal
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
/// <summary>
/// Run integration-tests against Redis.
/// </summary>
public class RedisTestInstance : IDisposable
{
    private static int _instanceCounter;
    internal static readonly ConcurrentBag<WeakReference<RedisTestInstance>> Instances = new();
    private readonly Process _process;
    private readonly TemporaryFile _executable;
    private readonly Config _config;

    public string Name { get; }

    internal bool IsDisposed { get; private set; }

    internal RedisTestInstance( [CallerMemberName] string? name = null )
    {
        var counter = Interlocked.Increment( ref _instanceCounter );
        this.Name = $"{counter}:{name}";

        Instances.Add( new WeakReference<RedisTestInstance>( this ) );

        Stream executableStream;
        string extension;

        if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            executableStream =
                typeof(IExecutablesResourceTarget).Assembly.GetManifestResourceStream( typeof(IExecutablesResourceTarget), "redis-server.exe" )!;

            extension = "exe";
        }
        else
        {
            executableStream = File.OpenRead( "/usr/bin/redis-server" );
            extension = "";
        }

        this._executable = new TemporaryFile( executableStream, "redis-test-", extension );

    restart:
        this._config = new Config();

        var processStartInfo = new ProcessStartInfo( this._executable.FileInfo.FullName )
        {
            UseShellExecute = false,
            Arguments =
                $"--port {this._config.Port} --bind 127.0.0.1 --appendonly no --save \"\" --maxmemory-policy volatile-lru --notify-keyspace-events AKE --dbfilename {this._executable.FileInfo.Name}.rdb",
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = Encoding.ASCII
        };

        var serverStartupEvent = new ManualResetEventSlim( false );
        var exited = false;
        var portError = false;
        string? lastLine = null;

        this._process = new Process() { StartInfo = processStartInfo };

        this._process.ErrorDataReceived +=
            ( _, eventArgs ) =>
            {
                if ( eventArgs.Data != null )
                {
                    this._config.Logger.Invoke( eventArgs.Data );
                }
            };

        // ReSharper disable AccessToModifiedClosure

        this._process.OutputDataReceived +=
            ( _, eventArgs ) =>
            {
                if ( eventArgs.Data == null )
                {
                    return;
                }

                // This not the best way, but it's a quick way to know it's properly started.
                if ( eventArgs.Data.IndexOf( $"The server is now ready to accept connections on port {this._config.Port}", StringComparison.Ordinal ) != -1 )
                {
                    serverStartupEvent.Set();
                }

                // This is to recognize port conflicts and restart quickly.
                if ( eventArgs.Data.IndexOf( $":{this._config.Port}: bind: No such file or directory", StringComparison.Ordinal ) != -1 )
                {
                    Volatile.Write( ref portError, true );
                    serverStartupEvent.Set();
                }

                Volatile.Write( ref lastLine, eventArgs.Data );
                this._config.Logger.Invoke( eventArgs.Data );
            };

        this._process.Exited +=
            ( _, _ ) =>
            {
                Volatile.Write( ref exited, true );
                serverStartupEvent.Set();
            };

        // ReSharper restore AccessToModifiedClosure

        try
        {
            this._process.Start();
        }
        catch ( Win32Exception e )
        {
            throw new Exception(
                $"Unable to start Redis test instance process (path={processStartInfo.FileName},exists={File.Exists( processStartInfo.FileName )},exception={e.Message},code={e.NativeErrorCode})." );
        }

        this._process.BeginOutputReadLine();

        for ( var i = 0; i < 30; i++ )
        {
            if ( serverStartupEvent.Wait( TimeSpan.FromSeconds( 1 ) ) )
            {
                goto waitingDone;
            }

            if ( this._process.HasExited )
            {
                Volatile.Write( ref exited, true );

                goto waitingDone;
            }
        }

        throw new Exception( $"Redis process did not start in time." );

    waitingDone:

        if ( Volatile.Read( ref portError ) )
        {
            if ( !this._process.HasExited )
            {
                try
                {
                    this._process.Kill();
                }
                catch ( Win32Exception ) { }
            }

            goto restart;
        }

        if ( Volatile.Read( ref exited ) )
        {
            throw new Exception( $"Redis process exited without being initialized properly. Last received line: {Volatile.Read( ref lastLine )}" );
        }
    }

    internal EndPoint Endpoint => new IPEndPoint( IPAddress.Loopback, this._config.Port );

    protected virtual void Dispose( bool disposing )
    {
        if ( this.IsDisposed )
        {
            return;
        }

        try
        {
            this._process.CancelOutputRead();
            this._process.StandardInput.Close();

            if ( !this._process.HasExited )
            {
                this._process.Kill();
            }

            if ( disposing )
            {
                this._process.Dispose();
                this._executable.Dispose();
            }
        }
        catch ( Exception ex )
        {
            this._config.Logger.Invoke( ex.ToString() );
        }

        this.IsDisposed = true;
    }

    ~RedisTestInstance()
    {
        this.Dispose( false );
    }

    public void Dispose()
    {
        this.Dispose( true );
        GC.SuppressFinalize( this );
    }

    private sealed class Config
    {
        private static readonly ConcurrentDictionary<int, byte> _usedPorts = new();
        private static readonly Random _random = new();

        internal Action<string> Logger { get; }

        internal int Port { get; }

        public Config()
        {
            do
            {
                this.Port = _random.Next( 49152, 65535 + 1 );
            }
            while ( _usedPorts.ContainsKey( this.Port ) );

            _usedPorts.AddOrUpdate( this.Port, _ => byte.MinValue, ( _, _ ) => byte.MinValue );
            this.Logger = TraceMessage;
        }

        private static void TraceMessage( string message )
        {
            try
            {
                Trace.WriteLine( message );
            }
            catch
            {
                // ignored
            }
        }
    }
}