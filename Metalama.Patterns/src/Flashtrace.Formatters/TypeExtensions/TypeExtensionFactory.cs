// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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