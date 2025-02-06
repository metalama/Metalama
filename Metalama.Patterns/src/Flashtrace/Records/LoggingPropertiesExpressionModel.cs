// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System.Globalization;

namespace Flashtrace.Records;

/// <summary>
/// Type of the <c>t</c> parameters for <c>if</c> and <c>sample</c> expressions in the policy configuration file.
/// </summary>
[PublicAPI]
public readonly struct LoggingPropertiesExpressionModel
{
    private readonly IReadOnlyList<LoggingProperty>? _properties;

    internal LoggingPropertiesExpressionModel( IReadOnlyList<LoggingProperty>? properties )
    {
        this._properties = properties;
    }

    /// <summary>
    /// Returns the value of the logging property with the given name, or null if there is no such logging property.
    /// </summary>
    /// <param name="key">Name of a logging property.</param>
    public object? this[ string key ]
    {
        get
        {
            if ( this._properties != null )
            {
                // ReSharper disable once ForCanBeConvertedToForeach : Original rationale now known, leaving unchanged.
                for ( var i = 0; i < this._properties.Count; i++ )
                {
                    if ( this._properties[i].Name.Equals( key, StringComparison.OrdinalIgnoreCase ) )
                    {
                        return this._properties[i].Value;
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Gets the value of a logging property as an Int32, or <paramref name="defaultValue"/> if the property does not exist. Throws an exception if the property
    /// exists but can't be converted to Int32 using <see cref="Convert.ToInt32(object)"/>.
    /// </summary>
    /// <param name="property">Name of a logging property.</param>
    /// <param name="defaultValue">The default value to return if the property does not exist.</param>
    public int GetInt32( string property, int defaultValue = 0 ) => Convert.ToInt32( this[property] ?? defaultValue, CultureInfo.InvariantCulture );

    /// <summary>
    /// Gets the value of a logging property as an Int64, or <paramref name="defaultValue"/> if the property does not exist. Throws an exception if the property
    /// exists but can't be converted to Int64 using <see cref="Convert.ToInt64(object)"/>.
    /// </summary>
    /// <param name="property">Name of a logging property.</param>
    /// <param name="defaultValue">The default value to return if the property does not exist.</param>
    public long GetInt64( string property, long defaultValue = 0 ) => Convert.ToInt64( this[property] ?? defaultValue, CultureInfo.InvariantCulture );

    /// <summary>
    /// Gets the value of a logging property as a string, or <paramref name="defaultValue"/> if the property does not exist.
    /// </summary>
    /// <param name="property">Name of a logging property.</param>
    /// <param name="defaultValue">The default value to return if the property does not exist.</param>
    public string GetString( string property, string defaultValue = "" )
        => Convert.ToString( this[property] ?? defaultValue, CultureInfo.InvariantCulture ) ?? defaultValue;

    /// <summary>
    /// Returns true if a logging property with the given name exists.
    /// </summary>
    /// <param name="property">Name of a logging property that might exist.</param>
    public bool Contains( string property ) => this[property] != null;
}