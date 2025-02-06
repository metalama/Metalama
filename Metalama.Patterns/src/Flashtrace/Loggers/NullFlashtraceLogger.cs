// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Contexts;
using Flashtrace.Options;
using Flashtrace.Records;
using JetBrains.Annotations;
using System.Diagnostics;

namespace Flashtrace.Loggers;

[UsedImplicitly]
public sealed partial class NullFlashtraceLogger : IFlashtraceLogger, IFlashtraceLocalLogger, IFlashtraceRoleLoggerFactory, ILoggingContext, ILogRecordBuilder,
                                                   IFlashtraceLoggerFactory
{
    private static bool _warningEmitted;

    [UsedImplicitly]
    public static NullFlashtraceLogger Instance { get; } = new();

    private NullFlashtraceLogger() { }

    FlashtraceRole IFlashtraceLogger.Role => null!;

    bool IFlashtraceLogger.RequiresSuspendResume => false;

    bool IFlashtraceLogger.IsEnabled( FlashtraceLevel level ) => false;

    bool IFlashtraceLocalLogger.IsEnabled( FlashtraceLevel level ) => false;

    void IFlashtraceLogger.Write(
        ILoggingContext? context,
        FlashtraceLevel level,
        LogRecordKind logRecordKind,
        string text,
        Exception? exception,
        in CallerInfo callerInfo )
        => EmitWarning( level );

    void IFlashtraceLogger.Write(
        ILoggingContext context,
        FlashtraceLevel level,
        LogRecordKind recordKind,
        string text,
        object[] args,
        Exception? exception,
        in CallerInfo callerInfo )
        => EmitWarning( level );

    ILoggingContext IFlashtraceLogger.OpenActivity( in LogActivityOptions options, in CallerInfo callerInfo ) => this;

    ILoggingContext IFlashtraceLocalLogger.OpenActivity( in OpenActivityOptions options, in CallerInfo callerInfo, bool isAsync ) => this;

    void IFlashtraceLogger.ResumeActivity( ILoggingContext context, in CallerInfo callerInfo ) { }

    void IFlashtraceLogger.SuspendActivity( ILoggingContext context, in CallerInfo callerInfo ) { }

    void IFlashtraceLocalLogger.ResumeActivity( ILoggingContext context, in CallerInfo callerInfo ) { }

    void IFlashtraceLocalLogger.SuspendActivity( ILoggingContext context, in CallerInfo callerInfo ) { }

    void IFlashtraceLocalLogger.SetWaitDependency( ILoggingContext context, object waited ) { }

    public ILoggingContext CurrentContext => null!;

    bool ILoggingContext.IsDisposed => false;

    int ILoggingContext.RecycleId => 0;

    bool ILoggingContext.IsAsync => false;

    string ILoggingContext.SyntheticId => null!;

    IFlashtraceRoleLoggerFactory IFlashtraceLogger.Factory => this;

    (IFlashtraceLocalLogger Logger, bool IsEnabled) IFlashtraceLogger.GetContextLocalLogger( FlashtraceLevel level ) => (this, false);

    private static void EmitWarning( FlashtraceLevel level )
    {
        if ( _warningEmitted || level < FlashtraceLevel.Warning )
        {
            return;
        }

        _warningEmitted = true;

        Trace.TraceWarning( "Flashtrace has not been initialized, but warnings and errors are being emitted and lost." );
    }

    void IFlashtraceLogger.SetWaitDependency( ILoggingContext context, object waited ) { }

    void IDisposable.Dispose() { }

#pragma warning disable SA1000 // Keywords should be spaced correctly
    void ILogRecordBuilder.WriteParameter<T>( int index, in ReadOnlySpan<char> parameterName, T? value, in LogParameterOptions options )
        where T : default { }
#pragma warning restore SA1000 // Keywords should be spaced correctly

    void ILogRecordBuilder.WriteString( in ReadOnlySpan<char> str ) { }

    void ILogRecordBuilder.SetException( Exception e ) { }

    void ILogRecordBuilder.SetExecutionTime( double executionTime, bool isOvertime ) { }

    ILogRecordBuilder IFlashtraceLocalLogger.GetRecordBuilder( in LogRecordOptions recordInfo, in CallerInfo callerInfo, ILoggingContext? context ) => this;

    void IFlashtraceExceptionHandler.OnInvalidUserCode( in CallerInfo callerInfo, string format, params object[] args ) { }

    void IFlashtraceExceptionHandler.OnInternalException( Exception exception ) { }

    void ILogRecordBuilder.Complete() { }

    void ILogRecordBuilder.BeginWriteItem( LogRecordItem item, in LogRecordTextOptions options ) { }

    void ILogRecordBuilder.EndWriteItem( LogRecordItem item ) { }

    IFlashtraceLocalLogger IFlashtraceLogger.GetContextLocalLogger() => this;

    IFlashtraceRoleLoggerFactory IFlashtraceLoggerFactory.ForRole( FlashtraceRole role ) => this;

    IFlashtraceLogger IFlashtraceRoleLoggerFactory.GetLogger( Type type ) => this;

    public IFlashtraceLogger GetLogger( string sourceName ) => this;
}