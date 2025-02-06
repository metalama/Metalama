// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters;
using JetBrains.Annotations;
using System.Globalization;
using System.Reflection;

namespace Metalama.Patterns.Caching.Formatters;

/// <summary>
/// Builds cache item keys and dependency keys. Implementation of <see cref="ICacheKeyBuilder"/>.
/// </summary>
[PublicAPI]
public class CacheKeyBuilder : IDisposable, ICacheKeyBuilder
{
    private readonly UnsafeStringBuilderPool _stringBuilderPool;

    /// <summary>
    /// Gets the formatters used to build the caching key.
    /// </summary>
    public IFormatterRepository Formatters { get; }

    /// <summary>
    /// Gets a sentinel object that means that the parameter is not a part of the cache key, and should be ignored.
    /// </summary>
    protected object IgnoredParameterSentinel { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheKeyBuilder"/> class specifying the maximal key size 
    /// and optionally a <see cref="IFormatterRepository"/>.
    /// </summary>
    /// <param name="maxKeySize">The maximal number of characters in cache keys.</param>
    /// <param name="formatterRepository">
    /// The <see cref="IFormatterRepository"/> from which to obtain formatters.
    /// </param>
    public CacheKeyBuilder( IFormatterRepository formatterRepository, CacheKeyBuilderOptions options )
    {
        this.Formatters = formatterRepository;
        this._stringBuilderPool = new UnsafeStringBuilderPool( options.MaxKeySize, true );
    }

    /// <summary>
    /// Gets the maximal number of characters in cache keys.
    /// </summary>
    public int MaxKeySize => this._stringBuilderPool.StringBuilderCapacity;

    /// <summary>
    /// Builds a cache key for a given method call.
    /// </summary>
    /// <param name="metadata">The <see cref="CachedMethodMetadata"/> representing the method.</param>
    /// <param name="instance">The <c>this</c> instance of the method call, or <c>null</c> if the method is static.</param>
    /// <param name="arguments">The arguments passed to the method call.</param>
    /// <returns>A string uniquely representing the method call.</returns>
    public virtual string BuildMethodKey( CachedMethodMetadata metadata, object? instance, IList<object?> arguments )
    {
        var method = metadata.Method;

        var parameters = method.GetParameters();

        if ( parameters.Length != arguments.Count )
        {
            throw new ArgumentOutOfRangeException(
                nameof(arguments),
                "The list must have the same number of items as the number of parameters of the method." );
        }

        switch ( method.IsStatic )
        {
            case false when instance == null:
                throw new ArgumentNullException( nameof(instance) );

            case true when instance != null:
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The {0} parameter must be null when {1} is a static method.",
                        nameof(instance),
                        nameof(method) ) );
        }

        // Compute the caching key.
        var stringBuilder = this._stringBuilderPool.GetInstance();

        this.AppendMethod( stringBuilder, method );
        stringBuilder.Append( '(' );

        var addComma = false;

        if ( !method.IsStatic && !metadata.IgnoreThisParameter )
        {
            // We need a 'this' specifier to differentiate an instance method
            // from a static method whose first parameter is of the declaring type.
            stringBuilder.Append( "this=" );
            this.AppendObject( stringBuilder, instance! );
            addComma = true;
        }

        for ( var i = 0; i < arguments.Count; i++ )
        {
            var argument = arguments[i];

            if ( metadata.IsParameterIgnored( i ) )
            {
                this.AppendArgument( stringBuilder, parameters[i].ParameterType, this.IgnoredParameterSentinel, ref addComma );
            }
            else
            {
                this.AppendArgument( stringBuilder, parameters[i].ParameterType, argument, ref addComma );
            }
        }

        stringBuilder.Append( ')' );
        var cacheKey = stringBuilder.ToString()!;
        this._stringBuilderPool.ReturnInstance( stringBuilder );

        return cacheKey;
    }

    /// <summary>
    /// Builds a dependency key for a given object.
    /// </summary>
    /// <param name="o">An object.</param>
    /// <returns>A dependency key that uniquely represents <paramref name="o"/>.</returns>
    public virtual string BuildDependencyKey( object o )
    {
        var stringBuilder = this._stringBuilderPool.GetInstance();
        this.AppendObject( stringBuilder, o );

        var key = stringBuilder.ToString()!;
        this._stringBuilderPool.ReturnInstance( stringBuilder );

        return key;
    }

    /// <summary>
    /// Appends the method name and generic arguments to an <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="stringBuilder">An <see cref="UnsafeStringBuilder"/>.</param>
    /// <param name="method">A <see cref="MethodInfo"/>.</param>
    protected virtual void AppendMethod( UnsafeStringBuilder stringBuilder, MethodInfo method )
    {
        this.AppendType( stringBuilder, method.DeclaringType! );
        stringBuilder.Append( '.' );
        stringBuilder.Append( method.Name );

        if ( method.IsGenericMethod )
        {
            this.AppendGenericArguments( stringBuilder, method.GetGenericArguments() );
        }
    }

    private void AppendArgument( UnsafeStringBuilder stringBuilder, Type parameterType, object? parameterValue, ref bool addComma )
    {
        if ( addComma )
        {
            stringBuilder.Append( ", " );
        }
        else
        {
            addComma = true;
        }

        this.AppendArgument( stringBuilder, parameterType, parameterValue );
    }

    /// <summary>
    /// Appends a method argument to an <see cref="UnsafeStringBuilder"/>. To avoid ambiguities between different overloads of the same method, the default implementation appends
    /// both the parameter type and the value key.
    /// </summary>
    /// <param name="stringBuilder">An <see cref="UnsafeStringBuilder"/>.</param>
    /// <param name="parameterType">The type of the parameter.</param>
    /// <param name="parameterValue">The value assigned to the parameter (can be <c>null</c>).</param>
    protected virtual void AppendArgument( UnsafeStringBuilder stringBuilder, Type parameterType, object? parameterValue )
    {
        // We need to include the parameter type to avoid ambiguities between overloads of the same method.
        stringBuilder.Append( '(' );
        this.AppendType( stringBuilder, parameterType );
        stringBuilder.Append( ')' );
        stringBuilder.Append( ' ' );

        if ( parameterValue == null )
        {
            stringBuilder.Append( "null" );
        }
        else
        {
            this.AppendObject( stringBuilder, parameterValue );
        }
    }

    /// <summary>
    /// Appends a <see cref="Type"/> name to an <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="stringBuilder">An <see cref="UnsafeStringBuilder"/>.</param>
    /// <param name="type">A <see cref="Type"/>.</param>
    protected virtual void AppendType( UnsafeStringBuilder stringBuilder, Type type ) => this.Formatters.Get<Type>().Format( stringBuilder, type );

    /// <summary>
    /// Appends a string representing an <see cref="object"/> to an <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="stringBuilder">An <see cref="UnsafeStringBuilder"/>.</param>
    /// <param name="o">An <see cref="object"/>.</param>
    protected virtual void AppendObject( UnsafeStringBuilder stringBuilder, object o )
    {
        if ( o == this.IgnoredParameterSentinel )
        {
            stringBuilder.Append( '*' );
        }
        else
        {
            var formatter = this.Formatters.Get( o.GetType() );
            formatter.Format( stringBuilder, o );
        }
    }

    private void AppendGenericArguments( UnsafeStringBuilder stringBuilder, Type[] genericArguments )
    {
        stringBuilder.Append( '<' );

        for ( var i = 0; i < genericArguments.Length; i++ )
        {
            if ( i > 0 )
            {
                stringBuilder.Append( ',' );
            }

            this.AppendType( stringBuilder, genericArguments[i] );
        }

        stringBuilder.Append( '>' );
    }

    /// <summary>
    /// Disposes the current object.
    /// </summary>
    /// <param name="disposing"><c>true</c> if the <see cref="Dispose()"/> method has been called, <c>false</c> if the object is being finalized by the garbage collector.</param>
    protected virtual void Dispose( bool disposing ) => this._stringBuilderPool.Dispose();

    /// <inheritdoc />
    public void Dispose() => this.Dispose( true );
}