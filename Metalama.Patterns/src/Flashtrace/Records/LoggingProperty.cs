// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System.Runtime.Serialization;

namespace Flashtrace.Records;

/// <summary>
/// Represents a property (a name, a value and a few options).
/// </summary>
[PublicAPI]
public sealed class LoggingProperty
{
    private readonly object? _value;
    private readonly Func<object>? _func;
#pragma warning disable IDE0032
    private LoggingPropertyOptions _options;
#pragma warning restore IDE0032

    /// <summary>
    /// Gets the property name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the property value. The property is never rendered when the value is <c>null</c>.
    /// If the <see cref="LoggingProperty"/> has been initialized with a <c>Func&lt;object&gt;</c>, this property
    /// will evaluate the delegate every time the property getter is invoked.
    /// </summary>
    public object? Value
    {
        get
        {
            if ( this._func != null )
            {
                return this._func();
            }
            else
            {
                return this._value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the formatter used to render the <see cref="Value"/> as a string. By default, the default formatter
    /// for the property value type is used.
    /// </summary>
    public IFormatter? Formatter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the property will be included in the log message. The default value is <c>false</c>.
    /// </summary>
    public bool IsRendered
    {
        get => this._options.IsRendered;
        set => this._options = this._options.WithIsRendered( value );
    }

    /// <summary>
    /// Gets or sets a value indicating whether the property is inherited from the parent activity to children activities and messages. The default value is <c>true</c>.
    ///  When this property is set to <c>false</c>, <see cref="IsBaggage"/> is automatically set to <c>false</c>.
    /// </summary>
    public bool IsInherited
    {
        get => this._options.IsInherited;
        set => this._options = this._options.WithIsInherited( value );
    }

    /// <summary>
    /// Gets or sets a value indicating whether the property is cross-process. The default value is <c>false</c>. When this property is set to <c>true</c>, <see cref="IsInherited"/> is automatically
    /// set to <c>true</c>.
    /// </summary>
    public bool IsBaggage
    {
        get => this._options.IsBaggage;
        set => this._options = this._options.WithIsBaggage( value );
    }

#pragma warning disable IDE0032
    internal LoggingPropertyOptions Options => this._options;
#pragma warning restore IDE0032

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingProperty"/> class specifying a constant value.
    /// </summary>
    /// <param name="name">Property name.</param>
    /// <param name="value">Property value.</param>
    public LoggingProperty( string name, object? value )
    {
        if ( string.IsNullOrWhiteSpace( name ) )
        {
            const string message = "The parameter '" + nameof(name) + "' is required.";

            throw name == null!
                ? new ArgumentNullException( nameof(name), message )
                : new ArgumentOutOfRangeException( nameof(name), message );
        }

        this.Name = name;
        this._value = value;
    }

    internal LoggingProperty( string name, object value, LoggingPropertyOptions options )
    {
        if ( string.IsNullOrWhiteSpace( name ) )
        {
            const string message = "The parameter '" + nameof(name) + "' is required.";

            throw name == null!
                ? new ArgumentNullException( nameof(name), message )
                : new ArgumentOutOfRangeException( nameof(name), message );
        }

        this.Name = name;
        this._value = value;
        this._options = options;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingProperty"/> class specifying a dynamic value.
    /// </summary>
    /// <param name="name">Property name.</param>
    /// <param name="func">A function returning the property value. This function will be evaluated every time the <see cref="Value"/> getter is invoked.</param>
    public LoggingProperty( string name, Func<object> func )
    {
        if ( string.IsNullOrWhiteSpace( name ) )
        {
            const string message = "The parameter '" + nameof(name) + "' is required.";

            throw name == null!
                ? new ArgumentNullException( nameof(name), message )
                : new ArgumentOutOfRangeException( nameof(name), message );
        }

        this.Name = name;
        this._func = func;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{this.Name}={this.Value}";
}