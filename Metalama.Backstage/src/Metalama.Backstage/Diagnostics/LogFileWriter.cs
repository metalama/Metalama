// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Metalama.Backstage.Diagnostics;

internal sealed class LogFileWriter
{
    private const int _inactiveStatus = 0;
    private const int _activeStatus = 1;
    private const int _finishingStatus = 2;
    private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    private readonly LoggerFactory _loggerFactory;
    private readonly IFileSystem _fileSystem;
    private readonly object _textWriterSync = new();
    private readonly ConcurrentQueue<string> _messageQueue = new();
    private TextWriter? _textWriter;
    private volatile int _backgroundTaskStatus;
    private volatile bool _disposing;

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string Scope { get; }

    public string LogFile { get; }

    public bool IsFailed { get; private set; }

    public LogFileWriter( LoggerFactory loggerFactory, string scope )
    {
        this._loggerFactory = loggerFactory;
        this._fileSystem = loggerFactory.FileSystem;
        this.Scope = scope;
        var scopeWithDash = string.IsNullOrEmpty( scope ) ? "" : "-" + scope;

        // The filename must be unique because several instances of the current assembly (of different versions) may be loaded in the process.
        this.LogFile = $"Metalama-{loggerFactory.ProcessKind}{scopeWithDash}-{Guid.NewGuid()}.log";
    }

    private void WriteBufferedMessagesToFile( object? state )
    {
        if ( this.IsFailed )
        {
            return;
        }

        if ( this._disposing )
        {
            this._backgroundTaskStatus = _inactiveStatus;

            return;
        }

        lock ( this._textWriterSync )
        {
            try
            {
                if ( this._textWriter == null )
                {
                    try
                    {
                        var directory = this._loggerFactory.GetLogDirectory();
                        var logFilePath = Path.Combine( directory, this.LogFile );
                        this._textWriter = this._fileSystem.CreateTextFile( logFilePath );
                    }
                    catch
                    {
                        // Don't fail if we cannot initialize the log.

                        this.IsFailed = true;

                        return;
                    }
                }

                // Process enqueued messages.
                var lastFlush = _stopwatch.ElapsedMilliseconds;

                while ( !this._messageQueue.IsEmpty )
                {
                    while ( this._messageQueue.TryDequeue( out var s ) )
                    {
                        this._textWriter.WriteLine( s );

                        // Flush at least every 250 ms.
                        if ( _stopwatch.ElapsedMilliseconds > lastFlush + 250 )
                        {
                            this._textWriter.Flush();
                            lastFlush = _stopwatch.ElapsedMilliseconds;
                        }
                    }

                    // Avoid too frequent start and stop of the task because this would also result in frequent flushing.
                    Thread.Sleep( 100 );
                }

                // Say that we are going to end the task.
                this._backgroundTaskStatus = _finishingStatus;

                // Process messages that may have been added in the meantime.
                while ( this._messageQueue.TryDequeue( out var s ) )
                {
                    this._textWriter.WriteLine( s );
                }

                this._textWriter.Flush();
            }
            catch ( ObjectDisposedException ) { }
            finally
            {
                this._backgroundTaskStatus = _inactiveStatus;
            }
        }
    }

    public void WriteLine( string s )
    {
        // Make sure that we are not starting a background writer thread after we start disposing.
        // Note that there can still be a race between Dispose in WriteLine, so we could still start a task, but we are
        // also checking the disposing flag in WriteBufferedMessagesToFile.
        if ( this._disposing )
        {
            return;
        }

        // If there was an error initializing the log file, do not attempt to write anything.
        if ( this.IsFailed )
        {
            return;
        }

        this._messageQueue.Enqueue( s );

        if ( Interlocked.CompareExchange( ref this._backgroundTaskStatus, _activeStatus, _inactiveStatus ) != _activeStatus )
        {
            ThreadPool.QueueUserWorkItem( this.WriteBufferedMessagesToFile );
        }
    }

    public void Flush()
    {
        if ( this.IsFailed )
        {
            return;
        }

        if ( this._backgroundTaskStatus != _inactiveStatus )
        {
            var spinWait = default(SpinWait);

            while ( this._backgroundTaskStatus != _inactiveStatus )
            {
                spinWait.SpinOnce();
            }
        }

        try
        {
            this._textWriter?.Flush();
        }
        catch ( ObjectDisposedException ) { }
    }

    public void Dispose()
    {
        // Make sure we write the lines that we already have.
        this.Flush();

        // Stop accepting new lines. This also prevents queued records to be processed. 
        this._disposing = true;

        this._textWriter?.Close();
    }
}