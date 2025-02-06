// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Flashtrace.Formatters.Utilities;

/// <summary>
/// A convenience wrapper for instances of anonymous types (or any unknown type) that exposes properties.
/// </summary>
[PublicAPI]
public readonly struct UnknownObjectAccessor : IEquatable<UnknownObjectAccessor>, IEnumerable<KeyValuePair<string, object?>>
{
    private readonly AccessorType? _type;
    private readonly object? _instance;

    private UnknownObjectAccessor( object? instance )
    {
        this._instance = instance;
        this._type = instance == null ? null : AccessorType.GetInstance( instance.GetType() );
    }

    private UnknownObjectAccessor( AccessorType type, object? instance )
    {
        this._type = type ?? throw new ArgumentNullException( nameof(type) );
        this._instance = instance;
    }

    /// <summary>
    /// Gets a delegate to a factory method that returns a <see cref="UnknownObjectAccessor"/> for an object of a type given as a generic parameter.
    /// </summary>
    /// <typeparam name="T">Type of the objects to be wrapped. It must be the final type (cannot be <c>object</c>, for instance).</typeparam>
    /// <returns>A delegate that takes the object as input and returns its <see cref="UnknownObjectAccessor"/>.</returns>
    public static Func<T, UnknownObjectAccessor> GetFactory<T>()
    {
        var type = AccessorType.GetInstance( typeof(T) );

        return o => new UnknownObjectAccessor( type, o );
    }

    /// <summary>
    /// Gets a delegate to a factory method that returns a <see cref="UnknownObjectAccessor"/> for an object of a type given as a generic parameter.
    /// </summary>
    /// <param name="type">Type of the objects to be wrapped. It must be the final type (cannot be <c>object</c>, for instance).</param>
    /// <returns>A delegate that takes the object as input and returns its <see cref="UnknownObjectAccessor"/>.</returns>
    public static Func<object?, UnknownObjectAccessor> GetFactory( Type type )
    {
        var accessorType = AccessorType.GetInstance( type );

        return o => new UnknownObjectAccessor( accessorType, o );
    }

    /// <summary>
    /// Gets an <see cref="UnknownObjectAccessor"/> for a specific object.
    /// </summary>
    /// <param name="value">The wrapped object.</param>
    /// <returns>A wrapper for <paramref name="value"/>.</returns>
    public static UnknownObjectAccessor GetInstance( object value ) => new( value );

    /// <summary>
    /// Gets the value of a named property for the current <see cref="UnknownObjectAccessor"/>.
    /// </summary>
    /// <typeparam name="T">Type of the property value.</typeparam>
    /// <param name="name">Property name.</param>
    /// <param name="value">Returns the property value if a property named <paramref name="name"/> exists and its value can be cast to <typeparamref name="T"/>, otherwise <c>default</c>.</param>
    /// <returns><c>true</c> if the property value if a property named <paramref name="name"/> exists and its value can be cast to <typeparamref name="T"/>, otherwise <c>false</c>.</returns>
    public bool TryGetProperty<T>( string name, out T value )
    {
        if ( this._type == null || this._instance == null )
        {
            value = default!;

            return false;
        }

        if ( this._type.TryGetProperty( this._instance, name, out var weaklyTypedValue ) )
        {
            if ( weaklyTypedValue is T typedValue )
            {
                value = typedValue;

                return true;
            }
        }

        value = default!;

        return false;
    }

    /// <summary>
    /// Gets the value of a named property for an arbitrary object.
    /// </summary>
    /// <typeparam name="T">Type of the property value.</typeparam>
    /// <param name="instance">The object whose property has to be returned.</param>
    /// <param name="name">Property name.</param>
    /// <param name="value">Returns the property value if a property named <paramref name="name"/> exists and its value can be cast to <typeparamref name="T"/>, otherwise <c>default</c>.</param>
    /// <returns><c>true</c> if the property value if a property named <paramref name="name"/> exists and its value can be cast to <typeparamref name="T"/>, otherwise <c>false</c>.</returns>
    public static bool TryGetProperty<T>( object instance, string name, out T value ) => GetInstance( instance ).TryGetProperty( name, out value );

    /// <summary>
    /// Converts the wrapped object to an array of name-value tuples.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<(string Name, object? Value)> ToTuples() => this._type?.ToTuples( this._instance ) ?? Array.Empty<(string Name, object? Value)>();

    /// <inheritdoc/>
    public override bool Equals( object? obj ) => obj is UnknownObjectAccessor other && this.Equals( other );

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return this._instance == null ? 0 : this._instance.GetHashCode();
    }

    /// <inheritdoc/>
    public bool Equals( UnknownObjectAccessor other )
    {
        return this._instance == other._instance;
    }

    /// <summary>
    /// Returns an <see cref="Enumerator"/>, which enumerates properties of the current <see cref="UnknownObjectAccessor"/>
    /// as a set of <c>KeyValuePair&lt;string, object&gt;</c>.
    /// </summary>
    /// <returns></returns>
    public Enumerator GetEnumerator() => new( this );

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => this.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    /// <summary>
    /// Invokes the <see cref="IUnknownObjectPropertyVisitor{TState}.Visit{TValue}(string, TValue, ref TState)"/> method of a given visitor
    /// for each property of the current <see cref="UnknownObjectAccessor"/>.
    /// </summary>
    /// <typeparam name="TState">The type of the opaque state passed to the  <see cref="IUnknownObjectPropertyVisitor{TState}.Visit{TValue}(string, TValue, ref TState)"/>
    /// method.</typeparam>
    /// <param name="visitor">The visitor.</param>
    /// <param name="state">The opaque state passed to the  <see cref="IUnknownObjectPropertyVisitor{TState}.Visit{TValue}(string, TValue, ref TState)"/> method.</param>
    public void VisitProperties<TState>( IUnknownObjectPropertyVisitor<TState> visitor, ref TState state )
    {
        if ( this._instance == null || this._type == null )
        {
            return;
        }

        this._type.VisitProperties( this._instance, visitor, ref state );
    }

    /// <summary>
    /// Determines if two <see cref="UnknownObjectAccessor"/> are equal.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==( UnknownObjectAccessor left, UnknownObjectAccessor right )
    {
        return left.Equals( right );
    }

    /// <summary>
    /// Determines if two <see cref="UnknownObjectAccessor"/> are different.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=( UnknownObjectAccessor left, UnknownObjectAccessor right ) => !(left == right);

    /// <summary>
    /// A value type that implements <see cref="IEnumerator{T}"/> of <see cref="KeyValuePair{TKey,TValue}"/> of <see cref="string"/> and <see cref="object"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<string, object?>>
    {
        private readonly object? _instance;
        private Dictionary<string, Func<object, object?>>.Enumerator _enumerator;

        internal Enumerator( in UnknownObjectAccessor accessor )
        {
            if ( accessor is { _instance: not null, _type: null } )
            {
                throw new ArgumentException( "State is not valid.", nameof(accessor) );
            }

            this._instance = accessor._instance;

            this._enumerator = accessor._type == null || accessor._instance == null
                ? default
                : accessor._type.GetAccessorEnumerator();
        }

        /// <inheritdoc/>
        public KeyValuePair<string, object?> Current
            => this._instance != null
                ? new KeyValuePair<string, object?>( this._enumerator.Current.Key, this._enumerator.Current.Value( this._instance ) )
                : default;

        object IEnumerator.Current => this._instance != null ? this.Current : null!;

        /// <inheritdoc/>
        public void Dispose()
        {
            if ( this._instance != null )
            {
                this._enumerator.Dispose();
            }
        }

        /// <inheritdoc/>
        public bool MoveNext() => this._instance != null && this._enumerator.MoveNext();

        /// <inheritdoc/>
        public void Reset() => throw new NotSupportedException();
    }

    public static IEnumerable<PropertyInfo> GetProperties( Type type )
    {
        foreach ( var property in type.GetRuntimeProperties() )
        {
            var getter = property.GetMethod;

            if ( getter != null && getter is { IsStatic: false, IsPublic: true } && getter.GetParameters().Length == 0 )
            {
                yield return property;
            }
        }
    }

    private class AccessorType
    {
        private static readonly ConcurrentDictionary<Type, AccessorType> _instances = new();
        private readonly Dictionary<string, Func<object, object?>> _accessors;
        private readonly ConcurrentDictionary<Type, object> _visitorListCache = new();
        private readonly Type _type;

        private AccessorType( Type type )
        {
            this._accessors = new Dictionary<string, Func<object, object?>>();

            foreach ( var property in GetProperties( type ) )
            {
                var param = Expression.Parameter( typeof(object) );
                var expression = Expression.Convert( Expression.Property( Expression.Convert( param, type ), property ), typeof(object) );
                var lambda = Expression.Lambda<Func<object, object?>>( expression, param );
                this._accessors.Add( property.Name, lambda.Compile() );
            }

            this._type = type;
        }

        public IReadOnlyList<(string Name, object? Value)> ToTuples( object? value )
        {
            if ( value == null )
            {
                return Array.Empty<(string Name, object? Value)>();
            }

            List<ValueTuple<string, object?>> properties = new( this._accessors.Count );

            foreach ( var accessor in this._accessors )
            {
                properties.Add( (accessor.Key, accessor.Value( value )) );
            }

            return properties;
        }

        [MustDisposeResource]
        internal Dictionary<string, Func<object, object?>>.Enumerator GetAccessorEnumerator() => this._accessors.GetEnumerator();

        public static AccessorType GetInstance( Type type )
            => _instances.GetOrAdd( type ?? throw new ArgumentNullException( nameof(type) ), t => new AccessorType( t ) );

        public bool TryGetProperty( object o, string name, out object? value )
        {
            if ( this._accessors.TryGetValue( name, out var func ) )
            {
                value = func( o );

                return true;
            }
            else
            {
                value = null;

                return false;
            }
        }

        public void VisitProperties<TState>( object o, IUnknownObjectPropertyVisitor<TState> visitor, ref TState state )
        {
            var actions =
                (Dictionary<string, VisitDelegate<TState>>) this._visitorListCache.GetOrAdd( typeof(TState), _ => this.GetVisitors<TState>() );

            foreach ( var action in actions )
            {
                if ( visitor.MustVisit( action.Key, ref state ) )
                {
                    try
                    {
                        action.Value( visitor, o, ref state );
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        private Dictionary<string, VisitDelegate<TState>> GetVisitors<TState>()
        {
            Dictionary<string, VisitDelegate<TState>> list = new();

            var visitMethod = typeof(IUnknownObjectPropertyVisitor<TState>).GetMethod( nameof(IUnknownObjectPropertyVisitor<TState>.Visit) )!;

            foreach ( var property in this._type.GetRuntimeProperties() )
            {
                var getter = property.GetMethod;

                if ( getter != null && getter is { IsStatic: false, IsPublic: true } && getter.GetParameters().Length == 0 )
                {
                    var visitorParameter = Expression.Parameter( typeof(IUnknownObjectPropertyVisitor<TState>) );
                    var objectParameter = Expression.Parameter( typeof(object) );
                    var stateParameter = Expression.Parameter( typeof(TState).MakeByRefType() );

                    var getValue = Expression.Property( Expression.Convert( objectParameter, this._type ), property );

                    var callExpression = Expression.Call(
                        visitorParameter,
                        visitMethod.MakeGenericMethod( property.PropertyType ),
                        Expression.Constant( property.Name ),
                        getValue,
                        stateParameter );

                    var lambda = Expression.Lambda<VisitDelegate<TState>>(
                        callExpression,
                        visitorParameter,
                        objectParameter,
                        stateParameter );

                    list.Add( property.Name, lambda.Compile() );
                }
            }

            return list;
        }

        private delegate void VisitDelegate<TState>( IUnknownObjectPropertyVisitor<TState> visitor, object instance, ref TState state );
    }
}