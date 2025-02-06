// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Contexts;
using Flashtrace.Options;
using Flashtrace.Records;
using JetBrains.Annotations;

namespace Flashtrace;

/// <summary>
/// Abstraction of the Logging facility, through which other components emit their log records.
/// </summary>
/// <remarks>
/// <para>If you want to implement this interface, you should also implement the <see cref="IFlashtraceRoleLoggerFactory"/> interface
/// and register it to the <see cref="IServiceProvider"/> or, if you use the dependency mechanism, to <see cref="FlashtraceSourceFactory"/>.
/// </para>
/// </remarks>
[PublicAPI]
public partial interface IFlashtraceLogger : IFlashtraceExceptionHandler
{
    /// <summary>
    /// Gets a factory that can provide instances of <see cref="IFlashtraceLogger"/>.
    /// </summary>
    IFlashtraceRoleLoggerFactory Factory { get; }

    /// <summary>
    /// Gets the role of records created by this <see cref="IFlashtraceLogger"/>. A list of standard roles is available in the <see cref="FlashtraceRole"/> class.
    /// </summary>
    FlashtraceRole Role { get; }

    /// <summary>
    /// Determines whether logging is enabled for a given <see cref="FlashtraceLevel"/>.
    /// </summary>
    /// <param name="level">A record level (or severity).</param>
    /// <returns><c>true</c> if logging is enabled for <paramref name="level"/>, otherwise <c>false</c>.</returns>
    bool IsEnabled( FlashtraceLevel level );

    /// <summary>
    /// Gets a value indicating whether calls of <see cref="SuspendActivity"/> and <see cref="ResumeActivity"/> 
    /// are required for asynchronous activities in the current context.
    /// </summary>
    bool RequiresSuspendResume { get; }

    /// <summary>
    /// Writes a log record with a description without parameters.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="level"><see cref="FlashtraceLevel"/> of the record.</param>
    /// <param name="recordKind">Kind of record.</param>
    /// <param name="text">Text of the record.</param>
    /// <param name="exception">The <see cref="Exception"/> associated with the record, or <c>null</c>.</param>
    /// <param name="callerInfo">Information about the caller source code.</param>
    void Write( ILoggingContext? context, FlashtraceLevel level, LogRecordKind recordKind, string text, Exception? exception, in CallerInfo callerInfo );

    /// <summary>
    /// Writes a log record with a description with an array of parameters.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="level"><see cref="FlashtraceLevel"/> of the record.</param>
    /// <param name="recordKind">Kind of record.</param>
    /// <param name="text">Text of the record.</param>
    /// <param name="args">An array of parameters.</param>
    /// <param name="exception">The <see cref="Exception"/> associated with the record, or <c>null</c>.</param>
    /// <param name="callerInfo">Information about the caller source code.</param>
    void Write(
        ILoggingContext context,
        FlashtraceLevel level,
        LogRecordKind recordKind,
        string text,
        object[] args,
        Exception? exception,
        in CallerInfo callerInfo );

    /// <summary>
    /// Opens an activity.
    /// </summary>
    /// <param name="options">Options.</param>
    /// <param name="callerInfo">Information about the caller source code.</param>
    /// <returns>An <see cref="ILoggingContext"/> representing the new activity.</returns>
    ILoggingContext OpenActivity( in LogActivityOptions options, in CallerInfo callerInfo );

    /// <summary>
    /// Resumes an asynchronous activity suspended by the <see cref="SuspendActivity"/> method.
    /// </summary>
    /// <param name="context">A context representing an asynchronous activity, created by <see cref="OpenActivity"/>
    /// and suspended by <see cref="SuspendActivity"/>.</param>
    /// <param name="callerInfo">Information about the caller source code.</param>
    void ResumeActivity( ILoggingContext context, in CallerInfo callerInfo );

    /// <summary>
    /// Suspends an asynchronous activity, which can then be resumed by the <see cref="ResumeActivity"/> method.
    /// </summary>
    /// <param name="context">A context representing an asynchronous activity, created by <see cref="OpenActivity"/>.</param>
    /// <param name="callerInfo">Information about the caller source code.</param>
    void SuspendActivity( ILoggingContext context, in CallerInfo callerInfo );

    /// <summary>
    /// Sets the wait dependency for a given context, i.e. give information about what the given context is waiting (or awaiting) for.
    /// </summary>
    /// <param name="context">The waiting context.</param>
    /// <param name="waited">The "thing" that is awaited for. Typically a <see cref="System.Threading.Tasks.Task"/>, or <c>TaskInfo</c>, or another context.</param>
    void SetWaitDependency( ILoggingContext context, object waited );

    /// <summary>
    /// Gets the current <see cref="ILoggingContext"/>.
    /// </summary>
    ILoggingContext CurrentContext { get; }

    /// <summary>
    /// Gets the logger for the current context.
    /// </summary>
    /// <returns></returns>
    IFlashtraceLocalLogger GetContextLocalLogger();

    /// <summary>
    /// Gets the <see cref="IFlashtraceLocalLogger"/> plus a flag indicating whether the source is enabled for a given <see cref="FlashtraceLevel"/>.
    /// </summary>
    /// <param name="level">Log level.</param>
    /// <returns>The <see cref="IFlashtraceLocalLogger"/> for the current execution context and a flag indicating whether logging
    /// is enabled for the given <paramref name="level"/>.</returns>
    (IFlashtraceLocalLogger Logger, bool IsEnabled) GetContextLocalLogger( FlashtraceLevel level );
}