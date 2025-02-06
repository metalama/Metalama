// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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