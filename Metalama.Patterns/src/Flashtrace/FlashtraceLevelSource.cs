// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Activities;
using Flashtrace.Contexts;
using Flashtrace.Messages;
using Flashtrace.Options;
using Flashtrace.Records;
using JetBrains.Annotations;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Flashtrace;

/// <summary>
/// A facade to a <see cref="FlashtraceSource"/> constrained to a specific <see cref="FlashtraceLevel"/>.
/// Instances of the <see cref="FlashtraceLevelSource"/> class are exposed as properties of the <see cref="FlashtraceSource"/>
/// class, e.g. <see cref="FlashtraceSource.Debug"/> or <see cref="FlashtraceSource.Error"/>.
/// </summary>
[PublicAPI]
[SuppressMessage(
    "StyleCop.CSharp.ReadabilityRules",
    "SA1124:Do not use regions",
    Justification = "Class has many method overloads which suit grouping by name." )]
public sealed class FlashtraceLevelSource
{
    internal FlashtraceLevelSource( FlashtraceSource flashtraceSource, FlashtraceLevel level )
    {
        this.Source = flashtraceSource;
        this.Level = level;
    }

    #region Write

    /// <summary>
    /// Writes a message.
    /// </summary>
    /// <typeparam name="T">Type of the <paramref name="message"/>.</typeparam>
    /// <param name="message">The message, typically created using the <see cref="SemanticMessageBuilder"/> or <see cref="FormattedMessageBuilder"/> class.</param>
    /// <param name="options">Options.</param>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public void Write<T>( in T message, in WriteMessageOptions options = default )
        where T : IMessage
    {
        var callerInfo = CallerInfo.GetDynamic( 1 );
        this.Write( message, null, options, callerInfo );
    }

    /// <excludeOverload />
    [EditorBrowsable( EditorBrowsableState.Never )]
    public void Write<T>( in T message, in WriteMessageOptions options, in CallerInfo callerInfo )
        where T : IMessage
    {
        this.Write( message, null, options, callerInfo );
    }

    /// <summary>
    /// Writes a message and specifies an <see cref="Exception"/>.
    /// </summary>
    /// <typeparam name="T">Type of the <paramref name="message"/>.</typeparam>
    /// <param name="message">The message, typically created using the <see cref="SemanticMessageBuilder"/> or <see cref="FormattedMessageBuilder"/> class.</param>
    /// <param name="exception">An optional <see cref="Exception"/>.</param>
    /// <param name="options">Options.</param>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public void Write<T>( in T message, Exception? exception, in WriteMessageOptions options = default )
        where T : IMessage
    {
        var callerInfo = CallerInfo.GetDynamic( 1 );
        this.Write( message, exception, options, callerInfo );
    }

    /// <excludeOverload />
    [EditorBrowsable( EditorBrowsableState.Never )]
    public void Write<T>( in T message, Exception? exception, in WriteMessageOptions options, in CallerInfo callerInfo )
        where T : IMessage
    {
        var (logger, isEnabled) = this.Source.Logger.GetContextLocalLogger( this.Level );

        if ( !isEnabled )
        {
            return;
        }

        try
        {
            using ( var recordBuilder = logger.GetRecordBuilder(
                       new LogRecordOptions( this.Level, LogRecordKind.Message, LogRecordAttributes.WriteMessage, options.Data ),
                       callerInfo ) )
            {
                MessageHelper.Write( message, recordBuilder, LogRecordItem.Message );

                if ( exception != null )
                {
                    recordBuilder.SetException( exception );
                }

                recordBuilder.Complete();
            }
        }
        catch ( Exception e )
        {
            logger.OnInternalException( e );
        }
    }

    #endregion

    #region OpenActivity

    // ReSharper disable InvalidXmlDocComment
#pragma warning disable CS1574, CS1584

    /// <summary>
    /// Opens an activity that cannot be suspended (e.g. does not wait any <c>await</c>).
    /// </summary>
    /// <param name="description">The activity description, typically created using the <see cref="SemanticMessageBuilder"/> or <see cref="FormattedMessageBuilder"/> class.</param>
    /// <param name="options">Options.</param>
    /// <returns>A <see cref="Activities.LogActivity{TActivityDescription}"/> representing the new activity.</returns>
    /// <remarks>The activity must be closed using
    /// <see cref="Activities.LogActivity{TActivityDescription}.SetSuccess(in CloseActivityOptions)"/>,
    /// <see cref="Activities.LogActivity{TActivityDescription}.SetResult{TResult}(TResult,in CloseActivityOptions)"/>,
    /// <see cref="Activities.LogActivity{TActivityDescription}.SetOutcome{TMessage}(FlashtraceLevel,in TMessage,Exception?,in CloseActivityOptions)"/>
    /// or <see cref="Activities.LogActivity{TActivityDescription}.SetException(Exception,in CloseActivityOptions)"/>.
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public LogActivity<T> OpenActivity<T>( in T description, in OpenActivityOptions options = default )
        where T : IMessage
    {
        var callerInfo = CallerInfo.GetDynamic( 1 );

        return this.OpenActivity( description, options, callerInfo );
    }

    /// <summary>
    /// Opens an activity that contains an <c>await</c>.
    /// </summary>
    /// <param name="description">The activity description, typically created using the <see cref="SemanticMessageBuilder"/> or <see cref="FormattedMessageBuilder"/> class.</param>
    /// <param name="options">Options.</param>
    /// <returns>A <see cref="Activities.LogActivity{TActivityDescription}"/> representing the new activity.</returns>
    /// <remarks>The activity must be closed using
    /// <see cref="Activities.LogActivity{TActivityDescription}.SetSuccess(in CloseActivityOptions)"/>
    /// <see cref="Activities.LogActivity{TActivityDescription}.SetResult{TResult}(TResult,in CloseActivityOptions)"/>,
    /// <see cref="Activities.LogActivity{TActivityDescription}.SetOutcome{TMessage}(FlashtraceLevel,in TMessage,Exception?,in CloseActivityOptions)"/>
    /// or <see cref="Activities.LogActivity{TActivityDescription}.SetException(Exception,in CloseActivityOptions)"/>.
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public LogActivity<T> OpenAsyncActivity<T>( in T description, in OpenActivityOptions options = default )
        where T : IMessage
    {
        var callerInfo = CallerInfo.GetDynamic( 1 );

        return this.OpenAsyncActivity( description, options, callerInfo );
    }
    
    // ReSharper restore InvalidXmlDocComment
#pragma warning restore CS1574, CS1584

    /// <excludeOverload />
    [EditorBrowsable( EditorBrowsableState.Never )]
    public LogActivity<T> OpenActivity<T>( in T description, in OpenActivityOptions options, in CallerInfo callerInfo )
        where T : IMessage
        => this.OpenActivity( description, options, callerInfo, false );

    [EditorBrowsable( EditorBrowsableState.Never )]
    public LogActivity<T> OpenAsyncActivity<T>( in T description, in OpenActivityOptions options, in CallerInfo callerInfo )
        where T : IMessage
        => this.OpenActivity( description, options, callerInfo, true );

    [EditorBrowsable( EditorBrowsableState.Never )]
    private LogActivity<T> OpenActivity<T>( in T description, in OpenActivityOptions options, in CallerInfo callerInfo, bool isAsync )
        where T : IMessage
    {
        try
        {
            var level = this.Level;
            IFlashtraceLocalLogger logger;
            bool isEnabled;

            ILoggingContext? context = null;

            if ( options.TransactionRequirement.RequiresTransaction )
            {
                // We need logger irrespective of whether it is enabled by verbosity. However, we need to respect other
                // ways to disable logging: disabled backend or disabled local configuration.
                level = level.WithForce();

                (logger, isEnabled) = this.Source.Logger.GetContextLocalLogger( level );

                if ( !isEnabled )
                {
                    // If even this is disabled, it means the source is hard disabled, then we won't open an activity.
                    return default;
                }

                // Do the actual opening:
                context = logger.OpenActivity( options with { Level = this.Level }, callerInfo, isAsync );

                // The log source has changed during OpenActivity because we're now within a ".Use()" call so we must update ourselves
                // to use the new log source:
                logger = this.Source.Logger.GetContextLocalLogger();
                isEnabled = true;
            }
            else if ( options.Data.HasInheritedProperty )
            {
                // We need to open a context anyway, except when it is "hard" disabled.
                (logger, isEnabled) = this.Source.Logger.GetContextLocalLogger( this.Level );

                if ( isEnabled )
                {
                    context = logger.OpenActivity( options, callerInfo, isAsync );
                }
                else
                {
                    context = logger.OpenActivity( options with { IsHidden = true }, callerInfo, isAsync );
                }
            }
            else
            {
                (logger, isEnabled) = this.Source.Logger.GetContextLocalLogger( this.Level );

                if ( isEnabled )
                {
                    context = logger.OpenActivity( options, callerInfo, isAsync );
                }
            }

            if ( isEnabled )
            {
                using ( var recordBuilder = logger.GetRecordBuilder(
                           new LogRecordOptions(
                               level,
                               LogRecordKind.ActivityEntry,
                               LogRecordAttributes.WriteActivityDescription,
                               default ),
                           callerInfo,
                           context ) )
                {
                    MessageHelper.Write( description, recordBuilder, LogRecordItem.ActivityDescription );
                    recordBuilder.Complete();
                }
            }

            return new LogActivity<T>( logger, new ActivityLevels( level, level.CopyForce( this.Source.FailureLevel ) ), context, description );
        }
        catch ( Exception e )
        {
            this.Source.Logger.OnInternalException( e );

            return default;
        }
    }

    #endregion

    #region LogActivity

    /// <summary>
    /// Executes an <see cref="Action"/> and logs its execution.
    /// </summary>
    /// <typeparam name="TDescription">The type of the description message.</typeparam>
    /// <param name="description">The activity description, typically created using the <see cref="SemanticMessageBuilder"/> or <see cref="FormattedMessageBuilder"/> class.</param>
    /// <param name="action">The action be be executed.</param>
    /// <param name="options">Options.</param>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public void LogActivity<TDescription>( in TDescription description, Action action, in OpenActivityOptions options = default )
        where TDescription : IMessage
    {
        var callerInfo = CallerInfo.GetDynamic( 1 );
        this.LogActivity( description, action, options, callerInfo );
    }

    /// <excludeOverload />
    [EditorBrowsable( EditorBrowsableState.Never )]
    public void LogActivity<TDescription>(
        in TDescription description,
        Action action,
        in OpenActivityOptions options,
        in CallerInfo callerInfo )
        where TDescription : IMessage
    {
        if ( action == null )
        {
            throw new ArgumentNullException( nameof(action) );
        }

        var activity = this.OpenActivity( description, options, callerInfo );

        try
        {
            action();
            activity.SetSuccess( default, callerInfo );
        }
        catch ( Exception e )
        {
            activity.SetException( e, default, callerInfo );

            throw;
        }
    }

    #endregion

    /// <summary>
    /// Executes a <see cref="Func{Result}"/> and logs its execution.
    /// </summary>
    /// <typeparam name="TDescription">The type of the description message.</typeparam>
    /// <typeparam name="TResult">The type of the return value of the action to execute.</typeparam>
    /// <param name="description">The activity description, typically created using the <see cref="SemanticMessageBuilder"/> or <see cref="FormattedMessageBuilder"/> class.</param>
    /// <param name="action">The action be be executed.</param>
    /// <param name="options">Options.</param>
    /// <returns>The return value of <paramref name="action"/>.</returns>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public TResult LogActivity<TDescription, TResult>( in TDescription description, Func<TResult> action, in OpenActivityOptions options = default )
        where TDescription : IMessage
    {
        var callerInfo = CallerInfo.GetDynamic( 1 );

        return this.LogActivity( description, action, options, callerInfo );
    }

    /// <excludeOverload />
    [EditorBrowsable( EditorBrowsableState.Never )]
    public TResult LogActivity<TDescription, TResult>(
        in TDescription description,
        Func<TResult> action,
        in OpenActivityOptions options,
        in CallerInfo callerInfo )
        where TDescription : IMessage
    {
        if ( action == null )
        {
            throw new ArgumentNullException( nameof(action) );
        }

        var activity = this.OpenActivity( description, options, callerInfo );

        try
        {
            var returnValue = action();
            activity.SetResult( returnValue, default, callerInfo );

            return returnValue;
        }
        catch ( Exception e )
        {
            activity.SetException( e, default, callerInfo );

            throw;
        }
    }

    /// <summary>
    /// Executes an asynchronous void delegate and logs its execution.
    /// </summary>
    /// <typeparam name="TDescription">The type of the description message.</typeparam>
    /// <param name="description">The activity description, typically created using the <see cref="SemanticMessageBuilder"/> or <see cref="FormattedMessageBuilder"/> class.</param>
    /// <param name="action">The action be be executed.</param>
    /// <param name="options">Options.</param>
    /// <returns>The <see cref="Task"/>.</returns>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public Task LogActivityAsync<TDescription>( in TDescription description, Func<Task> action, in OpenActivityOptions options = default )
        where TDescription : IMessage
    {
        return this.LogActivityAsyncImpl( description, action, options, CallerInfo.GetDynamic( 1 ) );
    }

    /// <excludeOverload />
    [EditorBrowsable( EditorBrowsableState.Never )]
    public Task LogActivityAsync<TDescription>( in TDescription description, Func<Task> action, in OpenActivityOptions options, in CallerInfo callerInfo )
        where TDescription : IMessage
    {
        return this.LogActivityAsyncImpl( description, action, options, callerInfo );
    }

    private async Task LogActivityAsyncImpl<TDescription>( TDescription description, Func<Task> action, OpenActivityOptions options, CallerInfo callerInfo )
        where TDescription : IMessage
    {
        if ( action == null )
        {
            throw new ArgumentNullException( nameof(action) );
        }

        var activity = this.OpenActivity( description, options, callerInfo );

        try
        {
            var task = action();

            if ( !task.IsCompleted )
            {
                activity.Suspend();

                try
                {
                    await task;
                }
                finally
                {
                    activity.Resume();
                }
            }

            activity.SetSuccess( default, callerInfo );
        }
        catch ( Exception e )
        {
            activity.SetException( e, default, callerInfo );

            throw;
        }
    }

    /// <summary>
    /// Executes an asynchronous non-void delegate and logs its execution.
    /// </summary>
    /// <typeparam name="TDescription">The type of the description message.</typeparam>
    /// <typeparam name="TResult">The type of the return value of the action to execute.</typeparam>
    /// <param name="description">The activity description, typically created using the <see cref="SemanticMessageBuilder"/> or <see cref="FormattedMessageBuilder"/> class.</param>
    /// <param name="action">The action be be executed.</param>
    /// <param name="options">Options.</param>
    /// <returns>A <see cref="Task{TResult}"/> whose result will be set to the result of <paramref name="action"/>.</returns>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public Task<TResult> LogActivityAsync<TDescription, TResult>(
        in TDescription description,
        Func<Task<TResult>> action,
        in OpenActivityOptions options = default )
        where TDescription : IMessage
    {
        return this.LogActivityAsyncImpl( description, action, options, CallerInfo.GetDynamic( 1 ) );
    }

    /// <excludeOverload />
    [EditorBrowsable( EditorBrowsableState.Never )]
    public Task<TResult> LogActivityAsync<TDescription, TResult>(
        in TDescription description,
        Func<Task<TResult>> action,
        in OpenActivityOptions options,
        in CallerInfo callerInfo )
        where TDescription : IMessage
    {
        return this.LogActivityAsyncImpl( description, action, options, callerInfo );
    }

    private async Task<TResult> LogActivityAsyncImpl<TDescription, TResult>(
        TDescription description,
        Func<Task<TResult>> action,
        OpenActivityOptions options,
        CallerInfo callerInfo )
        where TDescription : IMessage
    {
        if ( action == null )
        {
            throw new ArgumentNullException( nameof(action) );
        }

        var activity = this.OpenActivity( description, options, callerInfo );

        try
        {
            var task = action();

            if ( !task.IsCompleted )
            {
                activity.Suspend();

                try
                {
                    await task;
                }
                finally
                {
                    activity.Resume();
                }
            }

            activity.SetResult( task.Result, default, callerInfo );

            return task.Result;
        }
        catch ( Exception e )
        {
            activity.SetException( e, default, callerInfo );

            throw;
        }
    }

    internal FlashtraceSource Source { get; }

    /// <summary>
    /// Gets the <see cref="FlashtraceLevel"/> to which the current <see cref="FlashtraceLevelSource"/> is constrained.
    /// </summary>
    public FlashtraceLevel Level { get; }

    /// <summary>
    /// Gets a value indicating whether logging is enabled for the current <see cref="Level"/>.
    /// </summary>
    public bool IsEnabled => this.Source.Logger.IsEnabled( this.Level );

    /// <summary>
    /// Gets the current <see cref="FlashtraceLevelSource"/>, or <c>null</c> if logging is not enabled for the current instance. This
    /// property allows to write conditional logging using the <c>?.</c> operator.
    /// </summary>
    public FlashtraceLevelSource? IfEnabled => this.IsEnabled ? this : null;
}