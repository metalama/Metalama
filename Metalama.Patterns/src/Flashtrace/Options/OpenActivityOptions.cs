// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Activities;
using Flashtrace.Contexts;
using Flashtrace.Correlation;
using Flashtrace.Records;
using Flashtrace.Transactions;
using JetBrains.Annotations;

namespace Flashtrace.Options;

// TODO: Modernize. Consider readonly (record) struct and 'with' pattern for non-destructive mutability.
// ReSharper disable InvalidXmlDocComment
#pragma warning disable CS1574, CS1584
/// <summary>
/// Argument of the <see cref="FlashtraceLevelSource.OpenActivity{T}(in T, in OpenActivityOptions)"/> method and
/// related overloads.
/// </summary>
[PublicAPI]
public readonly struct OpenActivityOptions
{
    // ReSharper restore InvalidXmlDocComment
#pragma warning restore CS1574, CS1584

    private readonly short _kind;
    private readonly Flags _flags;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenActivityOptions"/> struct specifying properties using an arbitrary object 
    /// (possibly of an anonymous class).
    /// </summary>
    /// <param name="kind">Optional. The kind of activity.</param>
    /// <param name="data">Optional. Specifies the properties of the <see cref="OpenActivityOptions"/>, typically specified as an instance of a well-known or anonymous CLR type.
    /// The resulting <see cref="LogEventData"/> will have the default <see cref="LogEventMetadata"/>, which means that all CLR properties will be exposed
    /// as logging properties unless they are annotated with <see cref="LoggingPropertyOptionsAttribute"/>.</param>
    public OpenActivityOptions( object? data = null, LogActivityKind kind = default ) : this( LogEventData.Create( data ), kind ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenActivityOptions"/> struct specifying properties using a <see cref="LogEventData"/>.
    /// </summary>
    /// <param name="kind">Optional. The kind of activity.</param>
    /// <param name="data">Optional. Specifies the properties of the <see cref="WriteMessageOptions"/>. See <see cref="LogEventData"/>.</param>
    public OpenActivityOptions( in LogEventData data, LogActivityKind kind = default ) : this()
    {
        this.Kind = kind;
        this.Data = data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenActivityOptions"/> struct based on the specified <see cref="LogActivityOptions"/>.
    /// </summary>
    internal OpenActivityOptions( in LogActivityOptions options ) : this()
    {
        this._kind = (short) options.Kind;
    }

    internal bool IsHidden
    {
        get => (this._flags & Flags.IsHidden) != 0;
        init
        {
            var otherFlags = this._flags & ~Flags.IsHidden;
            var thisFlag = value ? Flags.IsHidden : Flags.Default;
            this._flags = otherFlags | thisFlag;
        }
    }

    /// <summary>
    /// Gets the logging options set by the remote caller of the request
    /// represented by the current <see cref="OpenActivityOptions"/>.
    /// </summary>
    public IncomingRequestOptions IncomingRequestOptions
    {
        get => new( (this._flags & Flags.IsParentSampled) != 0 );

        init
        {
            var otherFlags = this._flags & ~Flags.IsParentSampled;
            var thisFlag = value.IsParentSampled ? Flags.IsParentSampled : Flags.Default;
            this._flags = otherFlags | thisFlag;
        }
    }

    // ReSharper disable InvalidXmlDocComment
#pragma warning disable CS1574, CS1584

    /// <summary>
    /// Gets the <see cref="TransactionRequirement"/> for the current activity. These requirements can be set
    /// by the caller of <see cref="FlashtraceLevelSource.OpenActivity{T}(in T, in OpenActivityOptions)"/> or
    /// by <see cref="FlashtraceSource.ApplyTransactionRequirements(ref OpenActivityOptions)"/>.
    /// </summary>
    public TransactionRequirement TransactionRequirement { get; init; }

    // ReSharper restore InvalidXmlDocComment
#pragma warning restore CS1574, CS1584

    /// <summary>
    /// Gets a value indicating whether the resulting activity will be assigned a global id, irrespective of the id generation strategy.
    /// This means that the resulting <see cref="ILoggingContext.SyntheticId"/> will be rooted by  the current activity. When this property is <c>false</c>,
    /// the <see cref="ILoggingContext.SyntheticId"/> will may start with the id of the parent context. 
    /// </summary>
    public bool IsSyntheticRootId
    {
        get => (this._flags & Flags.IsRoot) != 0;

        init
        {
            var otherFlags = this._flags & ~Flags.IsRoot;
            var thisFlag = value ? Flags.IsRoot : Flags.Default;
            this._flags = otherFlags | thisFlag;
        }
    }

    /// <summary>
    /// Gets the kind of <see cref="LogActivity{TActivityDescription}"/>.
    /// </summary>
    public LogActivityKind Kind
    {
        get => (LogActivityKind) this._kind;
        init => this._kind = (short) value;
    }

    internal FlashtraceLevel Level
    {
        get => (FlashtraceLevel) ((short) this._flags >> 4);
        init => this._flags = (Flags) (((short) this._flags & ~0xf) | ((int) value << 4));
    }

    /// <summary>
    /// Gets the properties of the <see cref="OpenActivityOptions"/>, typically specified as an instance of a well-known or anonymous CLR type.
    /// </summary>
    public LogEventData Data { get; init; }

    /// <summary>
    /// Gets a specific value to use as the parent identifier part when building the <see cref="ILoggingContext.SyntheticId"/> property. When
    /// <see cref="SyntheticParentId"/> is null, <see cref="ILoggingContext.SyntheticId"/> is built recursively using the synthetic identifier
    /// based on the parent context. 
    /// </summary>
    public string? SyntheticParentId { get; init; }

    /// <summary>
    /// Gets a specific value to use for the <see cref="ILoggingContext.SyntheticId"/> property. When
    /// <see cref="SyntheticRootId"/> is null, <see cref="ILoggingContext.SyntheticId"/> is built recursively using the synthetic identifier
    /// based on the parent context. 
    /// </summary>
    public string? SyntheticRootId { get; init; }

    [Flags]
    private enum Flags : short
    {
        Default,
        IsHidden = 1,
        IsRoot = 2,
        IsParentSampled = 4

        // Top 4 bits (0xf0) represent the level.
    }
}