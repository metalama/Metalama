// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Contexts;
using Flashtrace.Options;
using Flashtrace.Records;
using JetBrains.Annotations;

namespace Flashtrace;

/// <summary>
/// Abstraction of logging methods where the context has already been resolved.
/// </summary>
[PublicAPI]
public interface IFlashtraceLocalLogger : IFlashtraceExceptionHandler
{
    /// <summary>
    /// Determines whether logging is enabled for a given <see cref="FlashtraceLevel"/>.
    /// </summary>
    /// <param name="level">A record level (or severity).</param>
    /// <returns><c>true</c> if logging is enabled for <paramref name="level"/>, otherwise <c>false</c>.</returns>
    bool IsEnabled( FlashtraceLevel level );

    /// <summary>
    /// Opens a new context for an activity.
    /// </summary>
    /// <param name="options">Options.</param>
    /// <param name="callerInfo">Information about the caller code.</param>
    /// <param name="isAsync"></param>
    /// <returns>A new context for the activity.</returns>
    ILoggingContext OpenActivity( in OpenActivityOptions options, in CallerInfo callerInfo, bool isAsync );

    /// <summary>
    /// Gets a record builder.
    /// </summary>
    /// <param name="options">Options.</param>
    /// <param name="callerInfo">Information about the caller code.</param>
    /// <param name="context">The context for which the record will be built, or <c>null</c> for standalone records.</param>
    /// <returns>A record builder.</returns>
    ILogRecordBuilder GetRecordBuilder( in LogRecordOptions options, in CallerInfo callerInfo, ILoggingContext? context = null );

    /// <summary>
    /// Resumes an asynchronous activity suspended by the <see cref="SuspendActivity"/> method.
    /// </summary>
    /// <param name="context">A context representing an asynchronous activity, created by <see cref="OpenActivity"/>
    /// and suspended by <see cref="SuspendActivity"/>.</param>
    /// <param name="callerInfo">Information about the caller code.</param>
    void ResumeActivity( ILoggingContext context, in CallerInfo callerInfo );

    /// <summary>
    /// Suspends an asynchronous activity, which can then be resumed by the <see cref="ResumeActivity"/> method.
    /// </summary>
    /// <param name="context">A context representing an asynchronous activity, created by <see cref="OpenActivity"/>.</param>
    /// <param name="callerInfo">Information about the caller code.</param>
    void SuspendActivity( ILoggingContext context, in CallerInfo callerInfo );

    /// <summary>
    /// Sets the wait dependency for a given context, i.e. give information about what the given context is waiting (or awaiting) for.
    /// </summary>
    /// <param name="context">The waiting context.</param>
    /// <param name="waited">The "thing" that is awaited for. Typically a <see cref="System.Threading.Tasks.Task"/>, or <c>TaskInfo</c>, or another context.</param>
    void SetWaitDependency( ILoggingContext context, object waited );
}