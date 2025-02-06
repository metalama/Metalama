// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Contexts;
using Flashtrace.Loggers;
using Flashtrace.Options;
using Flashtrace.Records;
using Flashtrace.Transactions;
using JetBrains.Annotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Flashtrace;

/// <summary>
/// Allow to write log messages and trace the execution of activities.
/// </summary>
/// <remarks>
/// <para>
/// Instances of this class are immutable. 
/// </para>
/// <para>
/// You should typically have one instance of this class per type.
/// Use <see cref="FlashtraceSource.Get()"/> to get an instance of this class for the current type.
/// </para>
/// </remarks>
[PublicAPI]
public sealed class FlashtraceSource
{
    public static FlashtraceSource Null { get; } = new( NullFlashtraceLogger.Instance, FlashtraceLevel.None, FlashtraceLevel.None );

    private FlashtraceLevelSource? _debugFlashtraceLevelSource;
    private FlashtraceLevelSource? _traceFlashtraceLevelSource;
    private FlashtraceLevelSource? _infoFlashtraceLevelSource;
    private FlashtraceLevelSource? _warningFlashtraceLevelSource;
    private FlashtraceLevelSource? _errorFlashtraceLevelSource;
    private FlashtraceLevelSource? _criticalFlashtraceLevelSource;
    private FlashtraceLevelSource? _noneFlashtraceLevelSource;

    internal FlashtraceSource(
        IFlashtraceLogger flashtraceLogger,
        FlashtraceLevel defaultLevel = FlashtraceLevel.Debug,
        FlashtraceLevel failureLevel = FlashtraceLevel.Error )
    {
        this.Logger = flashtraceLogger ?? throw new ArgumentNullException( nameof(flashtraceLogger) );
        this.DefaultLevel = defaultLevel;
        this.FailureLevel = failureLevel;
    }

    /// <inheritdoc cref="IFlashtraceLogger" />
    public ILoggingContext CurrentContext => this.Logger.CurrentContext;

    internal IFlashtraceLogger Logger { get; }

    /// <summary>
    /// Gets a new <see cref="FlashtraceSource"/> keeping all the configuration of the current instance, but for a different type.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> for which the <see cref="FlashtraceSource"/> should be created.</param>
    /// <returns>A <see cref="FlashtraceSource"/> for <paramref name="type"/>.</returns>
    public FlashtraceSource ForType( Type type )
    {
        if ( type == null )
        {
            throw new ArgumentNullException( nameof(type) );
        }

        return new FlashtraceSource( this.Logger.Factory.GetLogger( type ), this.DefaultLevel, this.FailureLevel );
    }

    /// <summary>
    /// Gets a new <see cref="FlashtraceSource"/> keeping all the configuration of the current instance, but for a source name.
    /// </summary>
    /// <param name="sourceName">The name of the source for which the <see cref="FlashtraceSource"/> should be created.</param>
    /// <returns>A <see cref="FlashtraceSource"/> for <paramref name="sourceName"/>.</returns>
    public FlashtraceSource ForSource( string sourceName )
    {
        if ( string.IsNullOrEmpty( sourceName ) )
        {
            throw new ArgumentNullException( nameof(sourceName) );
        }

        return new FlashtraceSource( this.Logger.Factory.GetLogger( sourceName ), this.DefaultLevel, this.FailureLevel );
    }

    /// <summary>
    /// Gets a new <see cref="FlashtraceSource"/> keeping all the configuration of the current instance, but for the calling type.
    /// </summary>
    /// <returns>A <see cref="FlashtraceSource"/> for the calling type.</returns>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public FlashtraceSource ForCurrentType()
    {
        var callerInfo = CallerInfo.GetDynamic( 1 );

        return this.ForType( callerInfo.SourceType! );
    }

    /// <excludeOverload />
    [EditorBrowsable( EditorBrowsableState.Never )]
    public FlashtraceSource ForCurrentType( in CallerInfo callerInfo )
    {
        return this.ForType( callerInfo.SourceType! );
    }

    /// <summary>
    /// Gets a <see cref="FlashtraceSource"/> for the calling type.
    /// </summary>
    /// <returns>A <see cref="FlashtraceSource"/> for the calling type.</returns>
    [MethodImpl( MethodImplOptions.NoInlining )]
    [Obsolete( "Use ILoggerFactory.GetLogSource." )]
    public static FlashtraceSource Get()
    {
        return Get( CallerInfo.GetDynamic( 1 ) );
    }

    /// <excludeOverload />
    [EditorBrowsable( EditorBrowsableState.Never )]
    [Obsolete( "Use ILoggerFactory.GetLogSource." )]
    public static FlashtraceSource Get( in CallerInfo callerInfo ) => FlashtraceSourceFactory.Default.GetFlashtraceSource( callerInfo );

    /// <summary>
    /// Gets a log source associated with a specific source name.
    /// </summary>
    /// <param name="sourceName">Name that the logging backend associates with a log source.</param>
    [Obsolete( "Use ILoggerFactory.GetLogSource." )]
    public static FlashtraceSource Get( string sourceName ) => FlashtraceSourceFactory.Default.GetFlashtraceSource( sourceName );

    /// <summary>
    /// Gets a log source for a specified role and source name.
    /// </summary>
    /// <param name="sourceName">The source name. A dotted name.</param>
    /// <param name="role">A role name. See <see cref="FlashtraceRole"/>.</param>
    /// <returns></returns>
    [Obsolete( "Use ILoggerFactory.GetLogSource." )]
    public static FlashtraceSource Get( string sourceName, FlashtraceRole? role )
        => role == null
            ? FlashtraceSourceFactory.Default.GetFlashtraceSource( sourceName )
            : FlashtraceSourceFactory.ForRole( role ).GetFlashtraceSource( sourceName );

    /// <summary>
    /// Gets a log source for a specified role and <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="role">See <see cref="FlashtraceRole"/>.</param>
    /// <returns></returns>
    [Obsolete( "Use ILoggerFactory.GetLogSource." )]
    public static FlashtraceSource Get( Type type, FlashtraceRole? role = null )
        => role == null
            ? FlashtraceSourceFactory.Default.GetFlashtraceSource( type )
            : FlashtraceSourceFactory.ForRole( role ).GetFlashtraceSource( type );

    /// <summary>
    /// Determines whether logging is enabled in the current <see cref="FlashtraceSource"/> for a given <see cref="FlashtraceLevel"/>.
    /// </summary>
    /// <param name="level">A <see cref="FlashtraceLevel"/>.</param>
    /// <returns><c>true</c> if logging is enabled for <paramref name="level"/>, otherwise <c>false</c>.</returns>
    public bool IsEnabled( FlashtraceLevel level )
    {
        try
        {
            return this.Logger.IsEnabled( level );
        }
        catch ( Exception e )
        {
            this.Logger.OnInternalException( e );

            return false;
        }
    }

    /// <summary>
    /// Exposes methods that allow to write messages and open activities with a specified <see cref="FlashtraceLevel"/>.
    /// </summary>
    /// <param name="level">The desired <see cref="FlashtraceLevel"/>.</param>
    /// <returns>A fluent API object that allows to write messages with the specified <paramref name="level"/>.</returns>
    /// <remarks>
    ///   <para>This method is properly optimized. Subsequent calls with the same parameter value will return the same instance.</para>
    /// </remarks>
    public FlashtraceLevelSource WithLevel( FlashtraceLevel level )
    {
        switch ( level )
        {
            case FlashtraceLevel.Trace:
                return this.Trace;

            case FlashtraceLevel.Debug:
                return this.Debug;

            case FlashtraceLevel.Info:
                return this.Information;

            case FlashtraceLevel.Warning:
                return this.Warning;

            case FlashtraceLevel.Error:
                return this.Error;

            case FlashtraceLevel.Critical:
                return this.Critical;

            default:
                return this.None;
        }
    }

    internal FlashtraceLevel DefaultLevel
    {
        get;
    }

    internal FlashtraceLevel FailureLevel
    {
        get;
    }

    /// <summary>
    /// Returns a new <see cref="FlashtraceSource"/> based on the current instance, but overrides the default logging levels.
    /// </summary>
    /// <param name="defaultLevel">The <see cref="FlashtraceLevel"/> for the <see cref="Default"/> severity (also used for the start and success messages of activities).</param>
    /// <param name="failureLevel">The <see cref="FlashtraceLevel"/> for the <see cref="Failure"/> severity (also used for the failure messages of activities).</param>
    /// <returns>A new <see cref="FlashtraceSource"/> with the specified log levels.</returns>
    public FlashtraceSource WithLevels( FlashtraceLevel defaultLevel, FlashtraceLevel failureLevel ) => new( this.Logger, defaultLevel, failureLevel );

    private FlashtraceLevelSource GetLevelSource( ref FlashtraceLevelSource? field, FlashtraceLevel level )
        => field ??= new FlashtraceLevelSource( this, level );

    /// <summary>
    /// Gets a <see cref="FlashtraceLevelSource"/> that can be used to write messages and open activities with the <see cref="FlashtraceLevel.None"/> severity. Such messages and activities are never displayed,
    /// but the activities can define properties that can be inherited to children activities and messages.
    /// </summary>
    /// <remarks>
    ///   <para>This property is properly optimized. Subsequent calls will return the same instance.</para>
    /// </remarks>
    public FlashtraceLevelSource None => this._noneFlashtraceLevelSource ??= new FlashtraceLevelSource( this, FlashtraceLevel.None );

    /// <summary>
    /// Gets a <see cref="FlashtraceLevelSource"/> that can be used to write messages and open activities with the default severity of the current <see cref="FlashtraceSource"/>. The default severity is <see cref="System.Diagnostics.Debug"/>,
    /// unless it has been overwritten by the <see cref="WithLevels(FlashtraceLevel, FlashtraceLevel)"/> method.
    /// </summary>
    /// <remarks>
    ///   <para>This property is properly optimized. Subsequent calls will return the same instance.</para>
    /// </remarks>
    public FlashtraceLevelSource Default => this.WithLevel( this.DefaultLevel );

    /// <summary>
    /// Gets a <see cref="FlashtraceLevelSource"/> that can be used to write messages and open activities with the default failure severity of the current <see cref="FlashtraceSource"/>. The default severity is <see cref="FlashtraceLevel.Error"/>,
    /// unless it has been overwritten by the <see cref="WithLevels(FlashtraceLevel, FlashtraceLevel)"/> method.
    /// </summary>
    /// <remarks>
    ///   <para>This property is properly optimized. Subsequent calls will return the same instance.</para>
    /// </remarks>
    public FlashtraceLevelSource Failure => this.WithLevel( this.FailureLevel );

    /// <summary>
    /// Gets a <see cref="FlashtraceLevelSource"/> that can be used to write messages and open activities with the <see cref="FlashtraceLevel.Critical"/> severity.
    /// </summary>
    /// <remarks>
    ///   <para>This property is properly optimized. Subsequent calls will return the same instance.</para>
    /// </remarks>
    public FlashtraceLevelSource Critical => this.GetLevelSource( ref this._criticalFlashtraceLevelSource, FlashtraceLevel.Critical );

    /// <summary>
    /// Gets a <see cref="FlashtraceLevelSource"/> that can be used to write messages and open activities with the <see cref="FlashtraceLevel.Error"/> severity.
    /// </summary>
    /// <remarks>
    ///   <para>This property is properly optimized. Subsequent calls will return the same instance.</para>
    /// </remarks>
    public FlashtraceLevelSource Error => this.GetLevelSource( ref this._errorFlashtraceLevelSource, FlashtraceLevel.Error );

    /// <summary>
    /// Gets a <see cref="FlashtraceLevelSource"/> that can be used to write messages and open activities with the <see cref="FlashtraceLevel.Warning"/> severity.
    /// </summary>
    /// <remarks>
    ///   <para>This property is properly optimized. Subsequent calls will return the same instance.</para>
    /// </remarks>
    public FlashtraceLevelSource Warning => this.GetLevelSource( ref this._warningFlashtraceLevelSource, FlashtraceLevel.Warning );

    /// <summary>
    /// Gets a <see cref="FlashtraceLevelSource"/> that can be used to write messages and open activities with the <see cref="FlashtraceLevel.Info"/> severity.
    /// </summary>
    /// <remarks>
    ///   <para>This property is properly optimized. Subsequent calls will return the same instance.</para>
    /// </remarks>
    public FlashtraceLevelSource Information => this.GetLevelSource( ref this._infoFlashtraceLevelSource, FlashtraceLevel.Info );

    /// <summary>
    /// Gets a <see cref="FlashtraceLevelSource"/> that can be used to write messages and open activities with the <see cref="FlashtraceLevel.Trace"/> severity.
    /// </summary>
    /// <remarks>
    ///   <para>This property is properly optimized. Subsequent calls will return the same instance.</para>
    /// </remarks>
    public FlashtraceLevelSource Trace => this.GetLevelSource( ref this._traceFlashtraceLevelSource, FlashtraceLevel.Trace );

    /// <summary>
    /// Gets a <see cref="FlashtraceLevelSource"/> that can be used to write messages and open activities with the <see cref="FlashtraceLevel.Debug"/> severity.
    /// </summary>
    /// <remarks>
    ///   <para>This property is properly optimized. Subsequent calls will return the same instance.</para>
    /// </remarks>
    public FlashtraceLevelSource Debug => this.GetLevelSource( ref this._debugFlashtraceLevelSource, FlashtraceLevel.Debug );

    /// <summary>
    /// Emits a log record with the source file and line of the caller.
    /// </summary>
    public void WriteExecutionPoint()
    {
        var callerInfo = CallerInfo.GetDynamic( 1 );

        if ( !callerInfo.SourceLineInfo.IsNull )
        {
            this.WriteExecutionPoint( callerInfo );
        }
    }

    /// <excludeOverload />
    [EditorBrowsable( EditorBrowsableState.Never )]
    public void WriteExecutionPoint( in CallerInfo callerInfo )
    {
        if ( this.IsEnabled( FlashtraceLevel.Debug ) )
        {
            this.Logger.Write( null, FlashtraceLevel.Debug, LogRecordKind.ExecutionPoint, "Executing", null, callerInfo );
        }
    }

    // ReSharper disable InvalidXmlDocComment
#pragma warning disable CS1574, CS1584

    /// <summary>
    /// Evaluates whether a transaction needs to be open for a specified <see cref="OpenActivityOptions"/> and updates
    /// its <see cref="OpenActivityOptions.TransactionRequirement"/> property.
    /// This method must be invoked before calling <see cref="FlashtraceLevelSource.OpenActivity{T}(in T, in OpenActivityOptions)"/>.
    /// It is not automatically called.
    /// </summary>
    /// <param name="openActivityOptions">The options of the activity to be opened. This method updates the 
    /// <see cref="OpenActivityOptions.TransactionRequirement"/> property of this parameter.</param>
    public void ApplyTransactionRequirements( ref OpenActivityOptions openActivityOptions )
    {
        if ( openActivityOptions.TransactionRequirement.RequiresTransaction )
        {
            // It has already been previously decided to open a transaction.
            return;
        }

        // ReSharper disable once SuspiciousTypeConversion.Global
        if ( this.Logger.GetContextLocalLogger() is ITransactionAwareContextLocalLogger logger )
        {
            openActivityOptions = logger.ApplyTransactionRequirements( openActivityOptions );
        }
    }

    // ReSharper restore InvalidXmlDocComment
#pragma warning restore CS1574, CS1584
}