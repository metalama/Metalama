// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Flashtrace.Records;

/// <summary>
/// Specifies the properties of a log event (such as a message, an activity opening or an activity closing).
/// Properties are typically passed in user code as an instance of an anonymous type. Any object can be provided. The properties
/// of the message then stem from the properties of the CLR object. To change the logic that maps the CLR object to
/// the list of property, you can specify a <see cref="LogEventMetadata"/>.
/// </summary>
[PublicAPI]
public readonly struct LogEventData : IEquatable<LogEventData>
{
    /// <summary>
    /// Gets the <see cref="LogEventMetadata"/>, which exposes the <see cref="Data"/> as a visitable set of name-value properties.
    /// </summary>
    /// <remarks>
    /// By design, <see cref="Metadata"/> will be non-null when <see cref="Data"/> is non-null.
    /// </remarks>
    public LogEventMetadata? Metadata { get; }

    /// <summary>
    /// Gets the raw CLR object. It must be interpreted using the <see cref="Metadata"/> property.
    /// </summary>
    public object? Data { get; }

    private LogEventData( object? data, LogEventMetadata? metadata = null )
    {
        this.Data = data;
        this.Metadata = metadata ?? (data == null ? null : LogEventMetadata.GetDefaultInstance( data.GetType() ));
    }

    /// <summary>
    /// Creates a new <see cref="LogEventData"/> and specifies a <see cref="LogEventMetadata"/>.
    /// </summary>
    /// <param name="data">The raw CLR object, typically an instance of anonymous type or any other type.</param>
    /// <param name="metadata">The <see cref="LogEventMetadata"/> used to interpret <paramref name="data"/>. When this parameter
    /// is <c>null</c>, the default <see cref="LogEventMetadata"/> implementation is used, which maps CLR properties into logging properties.</param>
    public static LogEventData Create( object? data, LogEventMetadata? metadata ) => new( data, metadata );

    /// <summary>
    /// Creates a new <see cref="LogEventData"/> and uses the default <see cref="LogEventMetadata"/> for the run-time type of the specified object.
    /// </summary>
    /// <param name="data">The raw CLR object, typically an instance of anonymous type or any other type.</param>
    public static LogEventData Create( object? data ) => new( data );

    /// <summary>
    /// Creates a new <see cref="LogEventData"/> and uses the default <see cref="LogEventMetadata"/> for the build-time type of the specified object.
    /// This overload is faster than the non-generic one.
    /// </summary>
    /// <param name="data">The raw CLR object, typically an instance of anonymous type or any other type.</param>
    public static LogEventData Create<T>( T? data ) => new( data, DefaultLogEventMetadata<T>.Instance );

    internal LogEventData( IReadOnlyList<LoggingProperty> properties )
    {
        this.Data = properties;
        this.Metadata = LogEventPropertiesMetadata.Instance;
    }

    /// <summary>
    /// Invokes an action for each property in the current <see cref="LogEventData"/>.
    /// </summary>
    /// <typeparam name="TVisitorState">The type of the <paramref name="visitorState"/> parameter, an opaque value passed to <paramref name="visitor"/>.</typeparam>
    /// <param name="visitor">The visitor.</param>
    /// <param name="visitorState">An opaque value passed to <paramref name="visitor"/>.</param>
    /// <param name="visitorOptions">Determines which properties need to be visited. By default, all properties are visited.</param>
    public void VisitProperties<TVisitorState>(
        ILoggingPropertyVisitor<TVisitorState> visitor,
        ref TVisitorState visitorState,
        in LoggingPropertyVisitorOptions visitorOptions = default )
    {
        if ( this.Data != null )
        {
            // If Data is not null, Metadata will not be null.
            this.Metadata!.VisitProperties( this.Data, visitor, ref visitorState, visitorOptions );
        }
    }

    /// <summary>
    /// Returns a dictionary containing all properties contained in the current <see cref="LogEventData"/>.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyDictionary<string, object?> ToDictionary()
    {
        Dictionary<string, object?> dictionary = new();
        this.VisitProperties( DictionaryVisitor.Instance, ref dictionary );

        return dictionary;
    }

    // Was [ExplicitCrossPackageInternal]
    public bool HasInheritedProperty => this.Data != null && this.Metadata!.HasInheritedProperty( this.Data );

    // Was [ExplicitCrossPackageInternal]
    public T? GetExpressionModel<T>() => this.Metadata == null ? default : this.Metadata.GetExpressionModel<T>( this.Data );

    /// <inheritdoc/>
    public override bool Equals( object? obj )
    {
        return obj is LogEventData other && this.Equals( other );
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return this.Data == null ? 0 : this.Data.GetHashCode();
    }

    /// <inheritdoc/>
    public bool Equals( LogEventData other )
    {
        return this.Data == other.Data;
    }

    /// <summary>
    /// Determines whether two instances of the <seealso cref="LogEventData"/> type are equal.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==( LogEventData left, LogEventData right )
    {
        return left.Equals( right );
    }

    /// <summary>
    /// Determines whether two instances of the <seealso cref="LogEventData"/> type are different.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=( LogEventData left, LogEventData right )
    {
        return !(left == right);
    }

    /// <summary>
    /// Returns a <see cref="LogEventData"/> that augments the current one with an additional property,
    /// but does not change the expression model.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="propertyName"></param>
    /// <param name="propertyValue"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public LogEventData WithAdditionalProperty<T>( string propertyName, T propertyValue, in LoggingPropertyOptions options = default )
        => AugmentedLogEventData.Augment( this, propertyName, propertyValue, options );

    /// <inheritdoc/>
    public override string ToString() => $"{{LogEventData {this.Data?.ToString() ?? "null"}}}";

    private class DictionaryVisitor : ILoggingPropertyVisitor<Dictionary<string, object?>>
    {
        public static readonly DictionaryVisitor Instance = new();

        public void Visit<TValue>( string name, TValue? value, in LoggingPropertyOptions options, ref Dictionary<string, object?> state )
        {
            state[name] = value;
        }
    }
}