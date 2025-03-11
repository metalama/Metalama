// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;

namespace Flashtrace.Formatters.TypeExtensions;

// T is for example IFormatter
// TContext is for example IFormatterRepository

/// <summary>
/// A TypeExtensionFactory for types deriving or implementing <typeparamref name="T"/>, where those types must have a constructor accepting a single argument of type <typeparamref name="TContext"/>.
/// </summary>
[PublicAPI] // TypeExtensionFactory<T> is public at least because Metalama.Patterns.Caching uses it. Making this type public too for consistency. 
public class TypeExtensionFactory<T, TContext> : TypeExtensionFactoryBase<T>
    where T : class
{
    private readonly object?[] _contextArray;

    // ReSharper disable once MemberCanBeProtected.Global
    public TypeExtensionFactory( Type genericInterfaceType, Type? converterType, Type? roleType, TContext? context )
        : base( genericInterfaceType, converterType, roleType )
    {
        this._contextArray = [context];
    }

    [return: NotNullIfNotNull( nameof(o) )]
    public T? Convert( T? o, Type targetObjectType ) => this.Convert( o, targetObjectType, this._contextArray );

    public void RegisterTypeExtension( Type targetType, Type typeExtensionType )
        => this.RegisterTypeExtension( targetType, typeExtensionType, this._contextArray );

    public TypeExtensionInfo<T> GetTypeExtension(
        Type objectType,
        TypeExtensionCacheUpdateCallback<T>? cacheUpdateCallback = null,
        Func<T?>? createDefault = null,
        Action<Exception>? onExceptionWhileCreatingTypeExtension = null )
        => this.GetTypeExtension(
            objectType,
            this._contextArray,
            cacheUpdateCallback,
            createDefault,
            onExceptionWhileCreatingTypeExtension );
}