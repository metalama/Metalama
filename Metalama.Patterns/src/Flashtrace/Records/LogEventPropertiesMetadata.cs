// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Flashtrace.Records;

internal sealed class LogEventPropertiesMetadata
    : LogEventMetadata<LoggingPropertiesExpressionModel>
{
    internal static readonly LogEventPropertiesMetadata Instance = new();

    private LogEventPropertiesMetadata() : base( "Properties" ) { }

    public override bool HasInheritedProperty( object? data )
    {
        if ( data == null )
        {
            return false;
        }

        var properties = (IReadOnlyList<LoggingProperty>) data;

        foreach ( var property in properties )
        {
            if ( property.IsInherited )
            {
                return true;
            }
        }

        return false;
    }

    public override void VisitProperties<TVisitorState>(
        object? data,
        ILoggingPropertyVisitor<TVisitorState> visitor,
        ref TVisitorState visitorState,
        in LoggingPropertyVisitorOptions visitorOptions = default )
    {
        if ( data == null )
        {
            return;
        }

        var properties = (IReadOnlyList<LoggingProperty>) data;

        foreach ( var property in properties )
        {
            if ( visitorOptions.OnlyInherited && !property.IsInherited )
            {
                continue;
            }

            visitor.Visit( property.Name, property.Value, property.Options, ref visitorState );
        }
    }

    public override LoggingPropertiesExpressionModel GetExpressionModel( object? data ) => new( (IReadOnlyList<LoggingProperty>?) data );
}