// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit.Abstractions;

namespace Metalama.Backstage.Testing;

public class TestLoggerFactory : ILoggerFactory
{
    private readonly ConcurrentDictionary<string, ILogger> _loggers = new();
    private readonly object _sync = new();
    private readonly ITestOutputHelper _testOutputHelper;

    // We use an immutable list to have thread-safe enumerations while items could still be added to the list.
    private ImmutableList<Entry> _entries = ImmutableList<Entry>.Empty;

    public TestLoggerFactory( ITestOutputHelper testOutputHelper )
    {
        this._testOutputHelper = testOutputHelper;
    }

    [UsedImplicitly]
    public void Clear()
    {
        lock ( this._sync )
        {
            this._entries = ImmutableList<Entry>.Empty;
        }
    }

    public IReadOnlyList<Entry> Entries => this._entries;

    public ILogger GetLogger( string category ) => this._loggers.GetOrAdd( category, c => new Logger( c, this ) );

    public void Flush() { }

    public IDisposable EnterScope( string scope ) => default(DisposableAction);

    public event Action<string>? MessageReported;

    private class Logger : ILogger
    {
        private readonly string _category;
        private readonly TestLoggerFactory _parent;

        public Logger( string category, TestLoggerFactory parent )
        {
            this._category = category;
            this._parent = parent;
            this.Trace = new LogWriter( this._category, Severity.Trace, this._parent );
            this.Info = new LogWriter( this._category, Severity.Info, this._parent );
            this.Warning = new LogWriter( this._category, Severity.Warning, this._parent );
            this.Error = new LogWriter( this._category, Severity.Error, this._parent );
        }

        public ILogWriter? Trace { get; set; }

        public ILogWriter? Info { get; set; }

        public ILogWriter? Warning { get; set; }

        public ILogWriter? Error { get; set; }

        public ILogger WithPrefix( string prefix ) => this._parent.GetLogger( this._category + "." + prefix );
    }

    private class LogWriter : ILogWriter
    {
        private readonly string _category;
        private readonly Severity _severity;
        private readonly TestLoggerFactory _parent;

        public LogWriter( string category, Severity severity, TestLoggerFactory parent )
        {
            this._category = category;
            this._severity = severity;
            this._parent = parent;
        }

        public void Log( string message )
        {
            lock ( this._parent._sync )
            {
                this._parent._entries = this._parent._entries.Add( new Entry( this._severity, message ) );
            }

            try
            {
                var formatted = $"{this._severity.ToString().ToUpperInvariant()} {this._category}: {message}";
                this._parent._testOutputHelper.WriteLine( formatted );
                this._parent.MessageReported?.Invoke( formatted );
            }
            catch
            {
                // There can be an exception if a message is reported during after the test finishes.
            }
        }
    }

    public enum Severity
    {
        Trace,
        Info,
        Warning,
        Error
    }

    public record Entry( Severity Severity, string Message );
}