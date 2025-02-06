// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters.Utilities;
using Flashtrace.Loggers;
using JetBrains.Annotations;
using System.Collections.Concurrent;
using System.Reflection;

namespace Flashtrace.Records;

/// <summary>
/// Defines how the raw CLR object stored in a <see cref="LogEventData"/> is translated into a set of visitable properties and an expression
/// that is accessible from the transaction policy expressions.
/// </summary>
[PublicAPI]
public class LogEventMetadata
{
    private static readonly LogEventMetadata _anonymous = new();

    private static readonly ConcurrentDictionary<Type, LogEventMetadata> _defaultInstances = new();

    private static LogEventMetadata GetDefaultInstanceImpl<T>() => DefaultLogEventMetadata<T>.Instance;

    internal static LogEventMetadata GetDefaultInstance( Type type )
    {
        return type.IsAnonymous()
            ? _anonymous
            : _defaultInstances.GetOrAdd(
                type,
                t => (LogEventMetadata) typeof(LogEventMetadata).GetMethod(
                        nameof(GetDefaultInstanceImpl),
                        BindingFlags.NonPublic | BindingFlags.Static )!
                    .MakeGenericMethod( t )
                    .Invoke( null, null )! );
    }

    /// <summary>
    /// Gets the name of the current <see cref="LogEventMetadata"/>. This property may be undefined. It must be defined 
    /// when the <see cref="LogEventData"/> must be available for evaluation from transaction policy expressions. In this case,
    /// the type of expression model (i.e. the generic parameter of <see cref="LogEventMetadata{T}"/>) must be identical for identical
    /// values of the <see cref="Name"/> property.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogEventMetadata"/> class.
    /// </summary>
    /// <param name="name">Optional. The name of the current <see cref="LogEventMetadata"/>. This property may be undefined. It must be defined 
    /// when the <see cref="LogEventData"/> must be available for evaluation from transaction policy expressions. In this case,
    /// the type of expression model (i.e. the generic parameter of <see cref="LogEventMetadata{T}"/>) must be identical for identical
    /// values of the <see cref="Name"/> property.</param>
    public LogEventMetadata( string? name = null )
    {
        this.Name = name;
    }

    /// <summary>
    /// Determines if the current <see cref="LogEventMetadata"/> contains any inherited property. The implementation of this method must not allocate heap memory.
    /// </summary>
    /// <param name="data">The raw CLR object, typically <see cref="LogEventData.Data" qualifyHint="true"/>.</param>
    /// <returns></returns>
    public virtual bool HasInheritedProperty( object? data ) => false;

    /// <summary>
    /// Gets the options of a given property.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns></returns>
    protected internal virtual LoggingPropertyOptions GetPropertyOptions( string name ) => default;

    internal virtual Type ExpressionModelType => typeof(object);

    /// <summary>
    /// Invokes an action for each property in the raw CLR object of a <see cref="LogEventData"/>.
    /// </summary>
    /// <typeparam name="TVisitorState">The type of the <paramref name="visitorState"/> parameter, an opaque value passed to <paramref name="visitor"/>.</typeparam>
    /// <param name="data">The raw CLR object, typically <see cref="LogEventData.Data" qualifyHint="true"/>.</param>
    /// <param name="visitor">The visitor.</param>
    /// <param name="visitorState">An opaque value passed to <paramref name="visitor"/>.</param>
    /// <param name="visitorOptions">Determines which properties need to be visited. By default, all properties are visited.</param>
    public virtual void VisitProperties<TVisitorState>(
        object? data,
        ILoggingPropertyVisitor<TVisitorState> visitor,
        ref TVisitorState visitorState,
        in LoggingPropertyVisitorOptions visitorOptions = default )
    {
        if ( data != null )
        {
            var accessor = UnknownObjectAccessor.GetInstance( data );

            PropertyVisitor<TVisitorState>.State ourState = new( this, visitorState, visitor, visitorOptions );

            accessor.VisitProperties( PropertyVisitor<TVisitorState>.Instance, ref ourState );

            visitorState = ourState.ChildState;
        }
    }

    internal T? GetExpressionModel<T>( object? data )
    {
        if ( this is LogEventMetadata<T> typedMetadata )
        {
            return typedMetadata.GetExpressionModel( data );
        }
        else
        {
            // This may throw an InvalidCastException, by design. That would be the fault of the caller.
            return (T?) data;
        }
    }

    private class PropertyVisitor<TChildState> : IUnknownObjectPropertyVisitor<PropertyVisitor<TChildState>.State>
    {
        public static readonly PropertyVisitor<TChildState> Instance = new();

        public bool MustVisit( string name, ref State state )
        {
            var options = state.Parent.GetPropertyOptions( name );

            if ( options.IsIgnored )
            {
                return false;
            }

            if ( state.Options.OnlyInherited && !options.IsInherited )
            {
                return false;
            }

            if ( state.Options.OnlyRendered && !options.IsRendered )
            {
                return false;
            }

            return true;
        }

        public void Visit<TValue>( string name, TValue value, ref State state )
        {
            var options = state.Parent.GetPropertyOptions( name );

            state.ChildVisitor.Visit( name, value, options, ref state.ChildState );
        }

        public struct State
        {
            public readonly LogEventMetadata Parent;
            public readonly ILoggingPropertyVisitor<TChildState> ChildVisitor;
            public readonly LoggingPropertyVisitorOptions Options;

            public TChildState ChildState;

            public State(
                LogEventMetadata parent,
                TChildState childState,
                ILoggingPropertyVisitor<TChildState> childVisitor,
                LoggingPropertyVisitorOptions options )
            {
                this.Parent = parent;
                this.ChildState = childState;
                this.ChildVisitor = childVisitor;
                this.Options = options;
            }
        }
    }
}