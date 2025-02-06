// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Contexts;
using Flashtrace.Loggers;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace Flashtrace;

/// <summary>
/// Extension methods for the <see cref="IFlashtraceRoleLoggerFactory"/> interface.
/// </summary>
[PublicAPI]
public static class FlashtraceExtensions
{
    private static readonly ConcurrentDictionary<CacheKey, FlashtraceSource> _cache = new();

    public static IServiceCollection AddFlashtrace( this IServiceCollection serviceCollection, bool throwOnMissingLoggerFactory = true )
    {
        serviceCollection.Add(
            ServiceDescriptor.Singleton<IFlashtraceLoggerFactory>(
                serviceProvider =>
                {
                    var loggerFactory =
                        throwOnMissingLoggerFactory
                            ? serviceProvider.GetRequiredService<ILoggerFactory>()
                            : serviceProvider.GetService<ILoggerFactory>();

                    if ( loggerFactory != null )
                    {
                        return new NetCoreSourceLoggerFactory( loggerFactory );
                    }
                    else
                    {
                        return NullFlashtraceLogger.Instance;
                    }
                } ) );

        return serviceCollection;
    }

    public static FlashtraceSource GetFlashtraceSource( this IServiceProvider? serviceProvider, Type type, FlashtraceRole? role = null )
    {
        var factory = (IFlashtraceLoggerFactory?) serviceProvider?.GetService( typeof(IFlashtraceLoggerFactory) );

        if ( factory == null )
        {
            return FlashtraceSource.Null;
        }
        else
        {
            return factory.ForRole( role ?? FlashtraceRole.Default ).GetFlashtraceSource( type );
        }
    }

    public static FlashtraceSource GetFlashtraceSource( this IServiceProvider? serviceProvider, string sourceName, FlashtraceRole? role = null )
    {
        var factory = (IFlashtraceLoggerFactory?) serviceProvider?.GetService( typeof(IFlashtraceLoggerFactory) );

        if ( factory == null )
        {
            return FlashtraceSource.Null;
        }
        else
        {
            return factory.ForRole( role ?? FlashtraceRole.Default ).GetFlashtraceSource( sourceName );
        }
    }

    public static FlashtraceSource GetFlashtraceSource( this IFlashtraceLoggerFactory? loggerFactory, Type type, FlashtraceRole? role = null )
    {
        if ( loggerFactory == null )
        {
            return FlashtraceSource.Null;
        }
        else
        {
            return loggerFactory.ForRole( role ?? FlashtraceRole.Default ).GetFlashtraceSource( type );
        }
    }

    public static FlashtraceSource GetFlashtraceSource( this IFlashtraceLoggerFactory? loggerFactory, string sourceName, FlashtraceRole? role = null )
    {
        if ( loggerFactory == null )
        {
            return FlashtraceSource.Null;
        }
        else
        {
            return loggerFactory.ForRole( role ?? FlashtraceRole.Default ).GetFlashtraceSource( sourceName );
        }
    }

    /// <summary>
    /// Gets a <see cref="FlashtraceSource"/> for a given role and <see cref="Type"/>.
    /// </summary>
    /// <param name="factory">An <see cref="IFlashtraceRoleLoggerFactory"/>.</param>
    /// <param name="type">The type that will emit the records.</param>
    /// <returns>A <see cref="FlashtraceSource"/> for <paramref name="type"/>.</returns>
    public static FlashtraceSource GetFlashtraceSource( this IFlashtraceRoleLoggerFactory factory, Type type )
    {
        if ( factory == null )
        {
            throw new ArgumentNullException( nameof(factory) );
        }

        if ( type == null )
        {
            throw new ArgumentNullException( nameof(type) );
        }

        var fullName = type.FullName ?? throw new ArgumentException(
            $"Must be a named type. For example, array types, generic type parameters and open generic types are not supported. {nameof(type)}.FullName must not return null.",
            nameof(type) );

        var cacheKey = new CacheKey( factory, fullName );

        if ( _cache.TryGetValue( cacheKey, out var result ) )
        {
            return result;
        }
        else
        {
            var newSource = new FlashtraceSource( factory.GetLogger( type ) );
            _cache.TryAdd( cacheKey, newSource );

            return newSource;
        }
    }

    /// <summary>
    /// Gets a <see cref="FlashtraceSource"/> for a given role and <paramref name="sourceName"/>.
    /// </summary>
    /// <param name="factory">An <see cref="IFlashtraceRoleLoggerFactory"/>.</param>
    /// <param name="sourceName">Log source name to be used by the backend. Not all backends support creating sources by name.</param>
    /// <returns>A <see cref="FlashtraceSource"/> for <paramref name="sourceName"/>.</returns>
    public static FlashtraceSource GetFlashtraceSource( this IFlashtraceRoleLoggerFactory factory, string sourceName )
    {
        if ( factory == null )
        {
            throw new ArgumentNullException( nameof(factory) );
        }

        if ( string.IsNullOrWhiteSpace( sourceName ) )
        {
            const string message = "The parameter '" + nameof(sourceName) + "' is required.";

            throw sourceName == null!
                ? new ArgumentNullException( nameof(sourceName), message )
                : new ArgumentOutOfRangeException( nameof(sourceName), message );
        }

        var cacheKey = new CacheKey( factory, sourceName );

        if ( _cache.TryGetValue( cacheKey, out var result ) )
        {
            return result;
        }
        else
        {
            var newSource = new FlashtraceSource( factory.GetLogger( sourceName ) );
            _cache.TryAdd( cacheKey, newSource );

            return newSource;
        }
    }

    /// <summary>
    /// Gets a <see cref="FlashtraceSource"/> for a given role and for the calling type.
    /// </summary>
    /// <param name="factory">An <see cref="IFlashtraceRoleLoggerFactory"/>.</param>
    /// <returns>A <see cref="FlashtraceSource"/> for the calling type.</returns>
    public static FlashtraceSource GetFlashtraceSource( this IFlashtraceRoleLoggerFactory factory )
    {
        if ( factory == null )
        {
            throw new ArgumentNullException( nameof(factory) );
        }

        return factory.GetFlashtraceSource( CallerInfo.GetDynamic( 1 ) );
    }

    /// <excludeOverload />
    [EditorBrowsable( EditorBrowsableState.Never )]
    public static FlashtraceSource GetFlashtraceSource( this IFlashtraceRoleLoggerFactory factory, in CallerInfo callerInfo )
    {
        if ( factory == null )
        {
            throw new ArgumentNullException( nameof(factory) );
        }

        // If we don't have a caller type, it's preferable to use System.Object as a safe fallback rather than throwing an exception.
        var callerType = callerInfo.SourceType ?? typeof(object);

        return factory.GetFlashtraceSource( callerType );
    }

    private readonly struct CacheKey : IEquatable<CacheKey>
    {
        private readonly object _factory;
        private readonly string _name;

        public CacheKey( object factory, string name )
        {
            this._factory = factory;
            this._name = name;
        }

        public override bool Equals( object? obj ) => obj is CacheKey entry && this.Equals( entry );

        public bool Equals( CacheKey other )
            => EqualityComparer<object>.Default.Equals( this._factory, other._factory ) &&
               this._name == other._name;

        public override int GetHashCode()
        {
            var hashCode = 163068155;
            hashCode = (hashCode * -1521134295) + EqualityComparer<object>.Default.GetHashCode( this._factory );
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode( this._name );

            return hashCode;
        }
    }
}