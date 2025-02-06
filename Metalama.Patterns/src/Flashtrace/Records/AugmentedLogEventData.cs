// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Flashtrace.Records;

internal static class AugmentedLogEventData
{
    private static readonly ConcurrentDictionary<(Type, Type), object> _delegateCache = new();

    public static LogEventData Augment<T>( in LogEventData augmented, string propertyName, T propertyValue, in LoggingPropertyOptions options )
    {
        var augmentedType = augmented.Metadata?.ExpressionModelType ?? typeof(object);

        var factory = (CreateMetaDataDelegate<T>) _delegateCache.GetOrAdd(
            (typeof(T), augmentedType),
            t => CreateFactory<T>( t.Item2 ) );

        var instance = factory( augmented, propertyName, propertyValue, options );

        return LogEventData.Create( instance, instance );
    }

    private static CreateMetaDataDelegate<TValue> CreateFactory<TValue>( Type augmentedType )
    {
        var augmentedParameter = Expression.Parameter( typeof(LogEventData).MakeByRefType() );
        var propertyNameParameter = Expression.Parameter( typeof(string) );
        var propertyValueParameter = Expression.Parameter( typeof(TValue) );
        var optionsParameter = Expression.Parameter( typeof(LoggingPropertyOptions).MakeByRefType() );

        var genericType = typeof(MetaData<,>).MakeGenericType( typeof(TValue), augmentedType );

        Expression newExpression = Expression.New(
            genericType.GetConstructors( BindingFlags.Instance | BindingFlags.Public )[0],
            augmentedParameter,
            propertyNameParameter,
            propertyValueParameter,
            optionsParameter );

        var lambda = Expression.Lambda<CreateMetaDataDelegate<TValue>>(
            newExpression,
            augmentedParameter,
            propertyNameParameter,
            propertyValueParameter,
            optionsParameter );

        return lambda.Compile();
    }

    private delegate LogEventMetadata CreateMetaDataDelegate<TValue>(
        in LogEventData augmented,
        string propertyName,
        TValue propertyValue,
        in LoggingPropertyOptions propertyOptions );

    // It's beneficial to have the data and metadata in the same object because we need an instance of both anyway.
    private sealed class MetaData<TValue, TAugmented> : LogEventMetadata<TAugmented>
    {
        private readonly LogEventData _augmented;

        private readonly string _propertyName;

        private readonly TValue _propertyValue;

        private readonly LoggingPropertyOptions _propertyOptions;

        public MetaData( in LogEventData augmented, string propertyName, TValue propertyValue, in LoggingPropertyOptions propertyOptions ) :
            base( augmented.Metadata?.Name )
        {
            this._augmented = augmented;
            this._propertyName = propertyName;
            this._propertyValue = propertyValue;
            this._propertyOptions = propertyOptions;
        }

        public override void VisitProperties<TVisitorState>(
            object? data,
            ILoggingPropertyVisitor<TVisitorState> visitor,
            ref TVisitorState visitorState,
            in LoggingPropertyVisitorOptions visitorOptions = default )
        {
            if ( !visitorOptions.OnlyInherited || this._propertyOptions.IsInherited )
            {
                visitor.Visit( this._propertyName, this._propertyValue, this._propertyOptions, ref visitorState );
            }

            this._augmented.VisitProperties( visitor, ref visitorState, visitorOptions );
        }

        public override bool HasInheritedProperty( object? data )
        {
            if ( !ReferenceEquals( data, this ) )
            {
                throw new FlashtraceAssertionFailedException();
            }

            if ( this._propertyOptions.IsInherited )
            {
                return true;
            }

            if ( this._augmented.Data != null )
            {
                // If this._augmented.Data is non-null, then this._augmented.Metadata must also be non-null. 
                return this._augmented.Metadata!.HasInheritedProperty( this._augmented.Data );
            }

            return false;
        }

        public override TAugmented? GetExpressionModel( object? data )
        {
            if ( !ReferenceEquals( data, this ) )
            {
                throw new FlashtraceAssertionFailedException();
            }

            return this._augmented.GetExpressionModel<TAugmented>();
        }
    }
}