// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Contexts;
using Flashtrace.Messages;
using Flashtrace.Options;
using JetBrains.Annotations;
using System.ComponentModel;

namespace Flashtrace.Activities;

/// <summary>
/// Exposes the public API of the generic value type <see cref="LogActivity{TActivityDescription}"/>.
/// </summary>
[PublicAPI]
public interface ILogActivity : IDisposable
{
    /// <summary>
    /// Gets the <see cref="ILoggingContext"/> corresponding to the current activity.
    /// </summary>
    ILoggingContext? Context { get; }

    /// <summary>
    /// Closes the activity with success and sets no other outcome message than <c>Succeeded</c>.
    /// </summary>
    /// <param name="options">Options.</param>
    void SetSuccess( in CloseActivityOptions options = default );

    /// <excludeOverload />
    [EditorBrowsable( EditorBrowsableState.Never )]
    void SetSuccess( in CloseActivityOptions options, in CallerInfo callerInfo );

    /// <summary>
    /// Closes the activity with success and includes a result in the outcome message.
    /// </summary>
    /// <param name="result">The result of the activity.</param>
    /// <param name="options">Options.</param>
    void SetResult<TResult>( TResult result, in CloseActivityOptions options = default );

    /// <excludeOverload />
    [EditorBrowsable( EditorBrowsableState.Never )]
    void SetResult<TResult>( TResult result, in CloseActivityOptions options, in CallerInfo callerInfo );

    /// <summary>
    /// Closes the activity and sets its outcome.
    /// </summary>
    /// <typeparam name="TMessage">Type of the <paramref name="message"/> parameter.</typeparam>
    /// <param name="level">Level of the outcome message.</param>
    /// <param name="message">The description of the activity outcome, typically created using the <see cref="SemanticMessageBuilder"/> or <see cref="FormattedMessageBuilder"/> class.</param>
    /// <param name="exception">An optional <see cref="System.Exception"/>.</param>
    /// <param name="options">Options.</param>
    void SetOutcome<TMessage>( FlashtraceLevel level, in TMessage message, Exception? exception = null, in CloseActivityOptions options = default )
        where TMessage : IMessage;

    /// <excludeOverload />
    [EditorBrowsable( EditorBrowsableState.Never )]
    void SetOutcome<TMessage>( FlashtraceLevel level, in TMessage message, Exception exception, in CloseActivityOptions options, in CallerInfo callerInfo )
        where TMessage : IMessage;

    /// <summary>
    /// Closes the activity with an <see cref="System.Exception"/>.
    /// </summary>
    /// <param name="exception">An <see cref="System.Exception"/>.</param>
    /// <param name="options">Options.</param>
    void SetException( Exception exception, in CloseActivityOptions options = default );

    /// <excludeOverload />
    [EditorBrowsable( EditorBrowsableState.Never )]
    void SetException( Exception exception, in CloseActivityOptions options, in CallerInfo callerInfo );

    /// <summary>
    /// Resumes the current async activity after it has been suspended by a call to <see cref="LogActivity{TActivityDescription}.Suspend()"/>. There is typically no need
    /// to invoke this method in user code because all async methods that use the <see cref="FlashtraceLevelSource"/> class are automatically instrumented.
    /// </summary>
    void Resume();

    /// <excludeOverload />
    [EditorBrowsable( EditorBrowsableState.Never )]
    void Resume( in CallerInfo callerInfo );

    /// <summary>
    /// Suspends the current async activity.
    /// The activity must than be resumed by a call of the <see cref="LogActivity{TActivityDescription}.Resume()"/> method.
    /// There is typically no need to invoke this method in user code because all async methods that use the <see cref="FlashtraceLevelSource"/> class are automatically instrumented.
    /// </summary>
    void Suspend();

    /// <excludeOverload />
    [EditorBrowsable( EditorBrowsableState.Never )]
    void Suspend( in CallerInfo callerInfo );
}