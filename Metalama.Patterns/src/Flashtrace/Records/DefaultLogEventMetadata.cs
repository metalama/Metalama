// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Formatters.Utilities;
using Flashtrace.Loggers;
using System.Reflection;

namespace Flashtrace.Records;

internal sealed class DefaultLogEventMetadata<T> : LogEventMetadata<T>
{
    private readonly Dictionary<string, LoggingPropertyOptions> _propertyOptions = new( StringComparer.OrdinalIgnoreCase );
    private readonly bool _hasInheritedProperty;

    public static readonly DefaultLogEventMetadata<T> Instance = new();

    private DefaultLogEventMetadata() : base( typeof(T).Name )
    {
        if ( !typeof(T).IsAnonymous() )
        {
            foreach ( var property in UnknownObjectAccessor.GetProperties( typeof(T) ) )
            {
                var attribute = property.GetCustomAttribute<LoggingPropertyOptionsAttribute>();

                if ( attribute != null )
                {
                    var options = attribute.ToOptions();
                    this._propertyOptions.Add( property.Name, options );

                    if ( options.IsInherited )
                    {
                        this._hasInheritedProperty = true;
                    }
                }
            }
        }
    }

    public override bool HasInheritedProperty( object? data )
    {
        // This implementation is a simplification because it would return true even if all
        // inherited properties are null. This should not matter because HasInheritedProperty is called to implement
        // an optimization.

        return this._hasInheritedProperty;
    }

    protected internal override LoggingPropertyOptions GetPropertyOptions( string name )
    {
        this._propertyOptions.TryGetValue( name, out var options );

        return options;
    }
}