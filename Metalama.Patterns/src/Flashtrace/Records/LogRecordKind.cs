// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Activities;
using Flashtrace.Options;
using JetBrains.Annotations;
using System.Collections;

namespace Flashtrace.Records;

// ReSharper disable InvalidXmlDocComment
/// <summary>
/// Kinds of log entry.
/// </summary>
[PublicAPI]
[Flags]
public enum LogRecordKind
{
    /// <summary>
    /// The value was not set.
    /// </summary>
    None = 0,

    /// <summary>
    /// Before method execution.
    /// </summary>
    MethodEntry = 1 << 1,

    /// <summary>
    /// After successful method execution.
    /// </summary>
    MethodSuccess = 1 << 2,

    /// <summary>
    /// After failed method execution (the method threw an exception).
    /// </summary>
    MethodException = 1 << 3,

    /// <summary>
    /// Before async method suspends execution.
    /// </summary>
    AsyncMethodAwait = 1 << 4,

    /// <summary>
    /// After async method resumes execution.
    /// </summary>
    AsyncMethodResume = 1 << 5,

    /// <summary>
    /// When value of a field or property changes.
    /// </summary>
    ValueChanged = 1 << 6,

    /// <summary>
    /// A general-purpose log message.
    /// </summary>
    /// <remarks>
    /// Emitted by:
    ///     <see cref="WriteMessageOptions"/> and related overloads.
    /// </remarks>
    Message = 1 << 7,

    /// <summary>
    /// Before an activity.
    /// </summary>
    /// <remarks>
    /// Emitted by:
    ///     <see cref="OpenActivityOptions"/> and
    ///     <see cref="LogActivity{TActivityDescription}"/> and related overloads.
    /// </remarks>
    ActivityEntry = 1 << 8,

    /// <summary>
    /// When an iterator yields a result.
    /// </summary>
    IteratorYield = 1 << 9,

    /// <summary>
    /// Before the <see cref="IEnumerator.MoveNext"/> method of an iterator executes.
    /// </summary>
    IteratorMoveNext = 1 << 10,

    /// <summary>
    /// Emitted by <see cref="FlashtraceSource.WriteExecutionPoint()"/>.
    /// </summary>
    /// <remarks>
    /// Emitted by:
    ///     <see cref="FlashtraceSource.WriteExecutionPoint()"/>.
    /// </remarks>
    ExecutionPoint = 1 << 11,

    /// <summary>
    /// After a method execution is successful but lasted more time than the threshold.
    /// </summary>
    MethodOvertime = 1 << 12,

// ReSharper disable InvalidXmlDocComment
#pragma warning disable CS1574, CS1584
    /// <summary>
    /// When a activity exits, regardless of success or failure.
    /// </summary>
    /// <remarks>
    /// Emitted by:
    ///     <see cref="LogActivity{TActivityDescription}.SetException(Exception, in CloseActivityOptions)"/>,
    ///     <see cref="LogActivity{TActivityDescription}.SetOutcome{TMessage}(FlashtraceLevel, in TMessage, Exception?, in CloseActivityOptions)"/>,
    ///     <see cref="LogActivity{TActivityDescription}.SetResult{TResult}(TResult, in CloseActivityOptions)"/> and
    ///     <see cref="LogActivity{TActivityDescription}.SetSuccess(in CloseActivityOptions)"/>.
    /// </remarks>
    ActivityExit = 1 << 13

    // ReSharper restore InvalidXmlDocComment
#pragma warning restore CS1574, CS1584
}