// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// A cache of <see cref="CachedMethodMetadata"/> objects keyed on <see cref="MethodInfo"/>.
/// </summary>
internal sealed class CachedMethodMetadataRegistry
{
    private readonly ConcurrentDictionary<MethodInfo, CachedMethodMetadata> _methodInfoCache = new();

    private CachedMethodMetadataRegistry() { }

    public static CachedMethodMetadataRegistry Instance { get; } = new();

    /// <summary>
    /// Gets the <see cref="CachedMethodMetadata"/> for a given <see cref="MethodInfo"/>, or <see langword="null"/> if no <see cref="CachedMethodMetadata"/> was registered for <paramref name="method"/>.
    /// </summary>
    /// <param name="method">A <see cref="MethodInfo"/>.</param>
    /// <returns>The <see cref="CachedMethodMetadata"/> for <paramref name="method"/>, or <c>null</c> if no <see cref="CachedMethodMetadata"/> was registered for <paramref name="method"/>.</returns>
    /// <remarks>
    /// If no <see cref="CachedMethodMetadata"/> has been registered for the given <paramref name="method"/>, this method will run the class constructor
    /// of the declaring type of the method then lookup the registration again.
    /// </remarks>
    internal CachedMethodMetadata? Get( MethodInfo method )
    {
        if ( !this._methodInfoCache.TryGetValue( method, out var cachedMethodInfo ) )
        {
            // Perhaps the declaring type has not been initialized. Try to initialize it ourselves.
            RuntimeHelpers.RunClassConstructor( method.DeclaringType!.TypeHandle );

            if ( !this._methodInfoCache.TryGetValue( method, out cachedMethodInfo ) )
            {
                // Original code threw a perhaps misleading "Declaring type of '{0}' method has not been initialized." exception
                // in this case. However, the documented behaviour (including original documented behaviour) is to
                // return null if not found. However, the original code would never return null.
                return null;
            }
        }

        return cachedMethodInfo;
    }

    public CachedMethodMetadata Register( CachedMethodMetadata metadata, bool throwIfAlreadyRegistered = false )
    {
        if ( !this._methodInfoCache.TryAdd( metadata.Method, metadata ) )
        {
            if ( throwIfAlreadyRegistered )
            {
                throw new InvalidOperationException( $"The method '{metadata.Method}' has already been registered." );
            }
            else
            {
                if ( !this._methodInfoCache.TryGetValue( metadata.Method, out var existingData ) )
                {
                    throw new CachingAssertionFailedException();
                }

                return existingData;
            }
        }
        else
        {
            return metadata;
        }
    }
}