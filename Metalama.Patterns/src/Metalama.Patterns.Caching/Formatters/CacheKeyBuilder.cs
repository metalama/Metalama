// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Formatters;
using JetBrains.Annotations;
using System.Globalization;
using System.IO.Hashing;
using System.Reflection;
using System.Text;

namespace Metalama.Patterns.Caching.Formatters;

/// <summary>
/// Default implementation of <see cref="ICacheKeyBuilder"/> that builds cache item keys and dependency keys.
/// </summary>
/// <remarks>
/// <h4>Key Building Strategy</h4>
/// <para>This class generates cache keys by combining method signature information with formatted argument values.</para>
/// <para><b>Method keys</b> follow this format:</para>
/// <code>Namespace.DeclaringType.MethodName&lt;GenericArgs&gt;(this={instance}, (ParamType) paramValue, ...)</code>
/// <para>For example: <c>MyApp.Services.UserService.GetUser(this={MyApp.Services.UserService}, (int) 42)</c></para>
/// <para><b>Dependency keys</b> are formatted representations of the dependency object using the registered formatters.</para>
///
/// <h4>Key Compression (Hashing)</h4>
/// <para>When <see cref="HashingAlgorithm"/> is set to <see cref="CacheKeyHashingAlgorithm.XxHash64"/> or
/// <see cref="CacheKeyHashingAlgorithm.XxHash128"/>, keys exceeding <see cref="KeyCompressingThreshold"/> characters
/// are compressed using a hash. This is useful for backends with key length limits (e.g., Redis, Memcached).</para>
/// <para><b>For method keys:</b> The class and method name are preserved as a prefix (with generic arguments stripped),
/// followed by a tilde (<c>~</c>) and the base64-encoded hash of the full key.</para>
/// <para>Example: <c>MyApp.Services.UserService.GetUser~9XwmlUowFDE</c></para>
/// <para><b>For dependency keys:</b> The entire key is replaced with the base64-encoded hash (no prefix).</para>
/// <para>XxHash64 produces ~11 character hashes; XxHash128 produces ~22 character hashes.</para>
///
/// <h4>Customization</h4>
/// <para>To customize key generation, either derive from this class and override the virtual methods,
/// or implement <see cref="ICacheKeyBuilder"/> directly. Register custom implementations using
/// <see cref="Building.ICachingServiceBuilder.WithKeyBuilder"/>.</para>
/// </remarks>
/// <seealso cref="ICacheKeyBuilder"/>
/// <seealso cref="CacheKeyBuilderOptions"/>
/// <seealso cref="CacheKeyHashingAlgorithm"/>
/// <seealso href="@caching-keys"/>
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
    /// Gets the algorithm used to compress the key if its length is above <see cref="KeyCompressingThreshold"/>.
    /// </summary>
    public CacheKeyHashingAlgorithm HashingAlgorithm { get; }

    /// <summary>
    /// Gets the length above which a key will be compressed if <see cref="HashingAlgorithm"/> is not <see cref="CacheKeyHashingAlgorithm.None"/>.
    /// </summary>
    public int KeyCompressingThreshold { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheKeyBuilder"/> class specifying the maximal key size
    /// and optionally a <see cref="IFormatterRepository"/>.
    /// </summary>
    /// <param name="formatterRepository">
    /// The <see cref="IFormatterRepository"/> from which to obtain formatters.
    /// </param>
    /// <param name="options">Options for the cache key builder.</param>
    public CacheKeyBuilder( IFormatterRepository formatterRepository, CacheKeyBuilderOptions options )
    {
        this.Formatters = formatterRepository;
        this._stringBuilderPool = new UnsafeStringBuilderPool( options.MaxKeySize, true );
        this.HashingAlgorithm = options.HashingAlgorithm;
        this.KeyCompressingThreshold = options.KeyCompressingThreshold;
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
        var cacheKey = this.CompressKey( stringBuilder, true, this.KeyCompressingThreshold );
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

        var key = this.CompressKey( stringBuilder, false, this.KeyCompressingThreshold );
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
    /// Gets the maximum length of the hash string for the specified algorithm.
    /// </summary>
    /// <param name="algorithm">The hashing algorithm.</param>
    /// <returns>The maximum hash string length (XxHash64 = 11, XxHash128 = 22).</returns>
    public static int GetHashLength( CacheKeyHashingAlgorithm algorithm )
        => algorithm switch
        {
            CacheKeyHashingAlgorithm.XxHash64 => 11,
            CacheKeyHashingAlgorithm.XxHash128 => 22,
            _ => 0
        };

    /// <summary>
    /// Hashes a buffer using <see cref="HashingAlgorithm"/> and appends the result to a <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="stringBuilder">The <see cref="StringBuilder"/> to append the hash to.</param>
    /// <param name="buffer">The buffer to hash.</param>
    protected virtual void AppendBufferHash( StringBuilder stringBuilder, ReadOnlySpan<byte> buffer )
    {
        switch ( this.HashingAlgorithm )
        {
            case CacheKeyHashingAlgorithm.XxHash64:
                {
                    var hash = new byte[8];
                    XxHash64.Hash( buffer, hash );
                    AppendBase64( stringBuilder, hash );

                    break;
                }

            case CacheKeyHashingAlgorithm.XxHash128:
                {
                    var hash = new byte[16];
                    XxHash128.Hash( buffer, hash );
                    AppendBase64( stringBuilder, hash );

                    break;
                }

            default:
                throw new InvalidOperationException( $"Unsupported hashing algorithm: {this.HashingAlgorithm}" );
        }
    }

    private static void AppendBase64( StringBuilder stringBuilder, byte[] bytes )
    {
        var base64 = Convert.ToBase64String( bytes );

        // Append without trailing '=' padding
        var length = base64.Length;

        while ( length > 0 && base64[length - 1] == '=' )
        {
            length--;
        }

        stringBuilder.Append( base64, 0, length );
    }

    /// <summary>
    /// Compresses the key if necessary.
    /// </summary>
    /// <param name="stringBuilder">The uncompressed key.</param>
    /// <param name="useMethodNameAsPrefix"><c>true</c> if the key is from <see cref="BuildMethodKey"/>, <c>false</c> when it is from <see cref="BuildDependencyKey"/>.
    /// In method keys, the method name is kept as a prefix, but parameters and type parameters are stripped.</param>
    /// <param name="threshold">The minimal key length above which compression is applied.</param>
    /// <returns>The compressed key.</returns>
    protected virtual unsafe string CompressKey( UnsafeStringBuilder stringBuilder, bool useMethodNameAsPrefix, int threshold )
    {
        if ( this.HashingAlgorithm != CacheKeyHashingAlgorithm.None && stringBuilder.Length > threshold )
        {
            var buffer = new ReadOnlySpan<byte>( (byte*) stringBuilder.Buffer, stringBuilder.Length * 2 );

            // Get the string to find the opening parenthesis.
            var keyString = stringBuilder.ToString()!;
            var indexOfParenthesis = keyString.IndexOfOrdinal( '(' );

            if ( useMethodNameAsPrefix && indexOfParenthesis > 0 )
            {
                var hashedString = new StringBuilder( indexOfParenthesis + 1 + GetHashLength( this.HashingAlgorithm ) );

                var genericDepthLevel = 0;

                for ( var i = 0; i < indexOfParenthesis; i++ )
                {
                    var c = keyString[i];

                    switch ( c )
                    {
                        case '<':
                            genericDepthLevel++;

                            break;

                        case '>':
                            genericDepthLevel--;

                            break;

                        default:
                            if ( genericDepthLevel == 0 )
                            {
                                hashedString.Append( c );
                            }

                            break;
                    }
                }

                hashedString.Append( '~' );
                this.AppendBufferHash( hashedString, buffer );

                return hashedString.ToString();
            }
            else
            {
                // For custom dependencies, we cannot automatically find a meaningful prefix because the string is freeform.
                // So we just return the hash without prefix.
                var hashedString = new StringBuilder( GetHashLength( this.HashingAlgorithm ) );
                this.AppendBufferHash( hashedString, buffer );

                return hashedString.ToString();
            }
        }
        else
        {
            return stringBuilder.ToString()!;
        }
    }

    /// <summary>
    /// Disposes the current object.
    /// </summary>
    /// <param name="disposing"><c>true</c> if the <see cref="Dispose()"/> method has been called, <c>false</c> if the object is being finalized by the garbage collector.</param>
    protected virtual void Dispose( bool disposing ) => this._stringBuilderPool.Dispose();

    /// <inheritdoc />
    public void Dispose() => this.Dispose( true );
}