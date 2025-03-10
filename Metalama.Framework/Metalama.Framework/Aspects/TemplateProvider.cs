// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// Wraps a template provider type or instance.
/// </summary>
[CompileTime]
public readonly record struct TemplateProvider
{
    private readonly object? _value;

    private TemplateProvider( object? instance )
    {
        this._value = instance;
    }

    public bool IsNull => this._value == null;

    /// <summary>
    /// Creates a <see cref="TemplateProvider"/> from an object instance.
    /// </summary>
    public static TemplateProvider FromInstance( ITemplateProvider? instance )
    {
        return new TemplateProvider( instance );
    }

    public static TemplateProvider FromInstanceUnsafe( object? instance ) => new( instance );

    /// <summary>
    /// Creates a <see cref="TemplateProvider"/> from a type.
    /// </summary>
    public static TemplateProvider FromType<T>()
        where T : ITemplateProvider
        => FromTypeUnsafe( typeof(T) );

    internal static TemplateProvider FromTypeUnsafe( Type type )
    {
        return new TemplateProvider( type );
    }

    internal object? Object => this._value is Type ? null : this._value;

    internal Type? Type => this._value is Type type ? type : this._value?.GetType();
}