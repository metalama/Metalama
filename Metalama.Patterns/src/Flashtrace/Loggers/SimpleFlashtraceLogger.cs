// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Contexts;
using Flashtrace.Messages;
using Flashtrace.Options;
using Flashtrace.Records;
using Flashtrace.Utilities;
using JetBrains.Annotations;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Flashtrace.Loggers;

/// <summary>
/// A base class for simple but low-performance and low-feature implementations of <see cref="IFlashtraceLogger"/>.
/// </summary>
/// <remarks>
/// <para>The simplification stems from the wrapping of all message arguments in an object array, which
/// allocates memory. Also, the logger does not support indentation.</para>
/// </remarks>
[PublicAPI]
public abstract partial class SimpleFlashtraceLogger : IFlashtraceLogger, IFlashtraceLocalLogger
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleFlashtraceLogger"/> class.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <param name="name">The source name.</param>
    protected SimpleFlashtraceLogger( FlashtraceRole role, string name )
    {
        this.Role = role;

        if ( string.IsNullOrEmpty( role.Name ) )
        {
            this.Category = name;
        }
        else
        {
            this.Category = role.Name + "." + name;
        }
    }

    public string Category { get; }

    public FlashtraceRole Role { get; }

    /// <inheritdoc/>
    public abstract bool IsEnabled( FlashtraceLevel level );

    public abstract IFlashtraceRoleLoggerFactory Factory { get; }

    private static string? GetRecordKindText( LogRecordKind recordKind )
    {
        switch ( recordKind )
        {
            case LogRecordKind.ActivityEntry:
                return "Starting";

            case LogRecordKind.ActivityExit:
                return "Returning";

            default:
                return null;
        }
    }

    /// <summary>
    /// Writes a text message.
    /// </summary>
    /// <param name="level"></param>
    /// <param name="recordKind"></param>
    /// <param name="text">The fully-rendered message.</param>
    /// <param name="exception">An optional <see cref="Exception"/>.</param>
    protected abstract void Write( FlashtraceLevel level, LogRecordKind recordKind, string text, Exception? exception );

    private void WriteFormatted( ILoggingContext? context, FlashtraceLevel level, LogRecordKind recordKind, string text, object?[]? args, Exception? exception )
    {
        var stringBuilder = new StringBuilder();

        var recordKindText = GetRecordKindText( recordKind );

        if ( recordKind == LogRecordKind.ActivityEntry )
        {
            WriteText( text, args, stringBuilder );

            if ( context is Context typedContext )
            {
                typedContext.Description = stringBuilder.ToString();
            }

            stringBuilder.Append( ": " );
            stringBuilder.Append( recordKindText );
        }
        else
        {
            if ( context != null )
            {
                stringBuilder.Append( ((Context) context).Description );
                stringBuilder.Append( ": " );

                if ( recordKindText != null )
                {
                    stringBuilder.Append( recordKindText );
                }
            }

            if ( !string.IsNullOrEmpty( text ) )
            {
                if ( stringBuilder.Length > 0 )
                {
                    stringBuilder.Append( ", " );
                }

                WriteText( text, args, stringBuilder );
            }
        }

        if ( exception != null )
        {
            stringBuilder.AppendLine();
            stringBuilder.Append( exception );
        }

        this.Write( level, recordKind, stringBuilder.ToString(), exception );
    }

    private static void WriteText( string text, object?[]? args, StringBuilder stringBuilder )
    {
        if ( args == null || args.Length == 0 )
        {
            stringBuilder.Append( text );
        }
        else
        {
            var parser = new FormattingStringParser( text.AsSpan() );

            var parameterIndex = 0;

            while ( true )
            {
                var str = parser.GetNextText();

                stringBuilder.PortableAppend( str );

                if ( !parser.TryGetNextParameter( out _ ) )
                {
                    break;
                }

                stringBuilder.Append( args[parameterIndex] );
                parameterIndex++;
            }
        }
    }

    void IFlashtraceLogger.Write(
        ILoggingContext context,
        FlashtraceLevel level,
        LogRecordKind recordKind,
        string text,
        object[] args,
        Exception? exception,
        in CallerInfo recordInfo )
    {
        if ( this.IsEnabled( level ) )
        {
            this.WriteFormatted( context, level, recordKind, text, args, exception );
        }
    }

    void IFlashtraceLogger.Write(
        ILoggingContext? context,
        FlashtraceLevel level,
        LogRecordKind recordKind,
        string text,
        Exception? exception,
        in CallerInfo recordInfo )
    {
        if ( this.IsEnabled( level ) )
        {
            this.WriteFormatted( context, level, recordKind, text, null, exception );
        }
    }

    ILoggingContext IFlashtraceLogger.OpenActivity( in LogActivityOptions options, in CallerInfo callerInfo ) => new Context( options.IsAsync );

    ILoggingContext IFlashtraceLocalLogger.OpenActivity( in OpenActivityOptions options, in CallerInfo callerInfo, bool isAsync ) => new Context( isAsync );

    void IFlashtraceLogger.ResumeActivity( ILoggingContext context, in CallerInfo callerInfo ) { }

    void IFlashtraceLogger.SuspendActivity( ILoggingContext context, in CallerInfo callerInfo ) { }

    void IFlashtraceLogger.SetWaitDependency( ILoggingContext context, object waited ) { }

    bool IFlashtraceLogger.RequiresSuspendResume => false;

    void IFlashtraceLocalLogger.ResumeActivity( ILoggingContext context, in CallerInfo callerInfo ) { }

    void IFlashtraceLocalLogger.SuspendActivity( ILoggingContext context, in CallerInfo callerInfo ) { }

    void IFlashtraceLocalLogger.SetWaitDependency( ILoggingContext context, object waited ) { }

    void IFlashtraceExceptionHandler.OnInvalidUserCode( in CallerInfo callerInfo, string format, params object[] args )
    {
        try
        {
            var message = "Error in user code calling the logging subsystem: " + string.Format( CultureInfo.InvariantCulture, format, args );

            message += Environment.NewLine + new StackTrace();

            this.WriteFormatted( null, FlashtraceLevel.Warning, LogRecordKind.Message, message, null, null );
        }

        // ReSharper disable once EmptyGeneralCatchClause
        catch { }
    }

    void IFlashtraceExceptionHandler.OnInternalException( Exception exception )
    {
        try
        {
            var message = "Error in user code calling the logging subsystem: " + exception;
            this.WriteFormatted( null, FlashtraceLevel.Warning, LogRecordKind.Message, message, null, null );
        }

        // ReSharper disable once EmptyGeneralCatchClause
        catch { }
    }

    ILogRecordBuilder IFlashtraceLocalLogger.GetRecordBuilder( in LogRecordOptions recordInfo, in CallerInfo callerInfo, ILoggingContext? context )
        => new RecordBuilder( this, recordInfo.Level, recordInfo.Kind, context as Context );

    IFlashtraceRoleLoggerFactory IFlashtraceLogger.Factory => this.Factory;

    IFlashtraceLocalLogger IFlashtraceLogger.GetContextLocalLogger() => this;

    (IFlashtraceLocalLogger Logger, bool IsEnabled) IFlashtraceLogger.GetContextLocalLogger( FlashtraceLevel level ) => (this, this.IsEnabled( level ));

    ILoggingContext IFlashtraceLogger.CurrentContext => throw new NotImplementedException();

    bool IFlashtraceLogger.IsEnabled( FlashtraceLevel level ) => this.IsEnabled( level );

    private class RecordBuilder : ILogRecordBuilder
    {
        private readonly StringBuilder _stringBuilder = new();
        private readonly FlashtraceLevel _level;
        private readonly LogRecordKind _recordKind;
        private readonly Context? _context;
        private readonly SimpleFlashtraceLogger? _flashtraceLogger;
        private Exception? _exception;
        private bool _appendComma;

        public RecordBuilder( SimpleFlashtraceLogger? flashtraceLogger, FlashtraceLevel level, LogRecordKind recordKind, Context? context )
        {
            this._flashtraceLogger = flashtraceLogger;
            this._level = level;
            this._recordKind = recordKind;
            this._context = context;
        }

        void ILogRecordBuilder.BeginWriteItem( LogRecordItem item, in LogRecordTextOptions options )
        {
            switch ( item )
            {
                case LogRecordItem.Message:
                case LogRecordItem.ActivityDescription:
                    if ( options.Name != null )
                    {
                        this._stringBuilder.Append( options.Name );
                    }

                    break;

                case LogRecordItem.ActivityOutcome:
                    if ( this._stringBuilder.Length > 0 )
                    {
                        this._stringBuilder.Append( ": " );
                    }
                    else if ( this._context != null )
                    {
                        this._stringBuilder.Append( this._context.Description );
                        this._stringBuilder.Append( ": " );
                    }

                    var text = options.Name ?? GetRecordKindText( this._recordKind );

                    if ( text != null )
                    {
                        this._stringBuilder.Append( text );
                        this._appendComma = true;
                    }

                    break;
            }
        }

        void ILogRecordBuilder.EndWriteItem( LogRecordItem item ) { }

        void ILogRecordBuilder.SetException( Exception e )
        {
            this._exception = e;
            this.WriteParameter( "exception", e, LogParameterOptions.SemanticParameter );
        }

        void ILogRecordBuilder.SetExecutionTime( double executionTime, bool isOvertime ) { }

        void ILogRecordBuilder.WriteParameter<T>( int index, in ReadOnlySpan<char> parameterName, T? value, in LogParameterOptions options )
            where T : default
            => this.WriteParameter( parameterName.ToString(), value, options );

        private void WriteParameter<T>( string parameterName, T value, in LogParameterOptions options )
        {
            switch ( options.Mode )
            {
                default:
                    this.WriteValue( value );

                    break;

                case LogParameterMode.NameValuePair:
                    this._stringBuilder.Append( ", " );
                    this._stringBuilder.Append( parameterName );
                    this._stringBuilder.Append( " = " );
                    this.WriteValue( value );

                    break;

                case LogParameterMode.Hidden:
                    break;
            }
        }

        private void WriteValue<T>( T value )
        {
            // ReSharper disable once CompareNonConstrainedGenericWithNull
            this._stringBuilder.Append( value == null ? "null" : value.ToString() );
        }

        void ILogRecordBuilder.WriteString( in ReadOnlySpan<char> span )
        {
            if ( this._appendComma )
            {
                this._appendComma = false;
                this._stringBuilder.Append( ", " );
            }

            this._stringBuilder.PortableAppend( span );
        }

        void ILogRecordBuilder.Complete()
        {
            if ( this._flashtraceLogger != null )
            {
                if ( this._recordKind == LogRecordKind.ActivityEntry )
                {
                    if ( this._context != null )
                    {
                        this._context.Description = this._stringBuilder.ToString();
                    }

                    this._stringBuilder.Append( ": " );
                    this._stringBuilder.Append( GetRecordKindText( LogRecordKind.ActivityEntry ) );
                }

                this._flashtraceLogger.Write( this._level, this._recordKind, this._stringBuilder.ToString(), this._exception );
            }
        }

        void IDisposable.Dispose() { }
    }

    private class Context : ILoggingContext
    {
        public Context( bool isAsync )
        {
            this.IsAsync = isAsync;
        }

        public string? Description
        {
            get;
            set;
        }

        public bool IsAsync { get; }

        public string? SyntheticId => null;

        public bool IsDisposed { get; private set; }

        int ILoggingContext.RecycleId => 0;

        void IDisposable.Dispose() => this.IsDisposed = true;
    }
}