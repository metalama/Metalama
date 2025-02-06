// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Flashtrace.Formatters.TypeExtensions;

// T is for example IFormatter

/// <summary>
/// A TypeExtensionFactory for types deriving or implementing <typeparamref name="T"/>, where those types must have a parameterless constructor.
/// </summary>
[PublicAPI] // Used by Metalama.Patterns.Caching. 
public class TypeExtensionFactory<T> : TypeExtensionFactoryBase<T>
    where T : class
{
    public TypeExtensionFactory( Type genericInterfaceType, Type? converterType, Type? roleType ) : base( genericInterfaceType, converterType, roleType ) { }

    public T? Convert( T? o, Type targetObjectType ) => this.Convert( o, targetObjectType, null );

    public void RegisterTypeExtension( Type targetType, Type typeExtensionType ) => this.RegisterTypeExtension( targetType, typeExtensionType, null );

    public TypeExtensionInfo<T> GetTypeExtension(
        Type objectType,
        TypeExtensionCacheUpdateCallback<T>? cacheUpdateCallback = null,
        Func<T?>? createDefault = null,
        Action<Exception>? onExceptionWhileCreatingTypeExtension = null )
        => this.GetTypeExtension( objectType, null, cacheUpdateCallback, createDefault, onExceptionWhileCreatingTypeExtension );
}