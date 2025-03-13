// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.UserCode;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;

namespace Metalama.Framework.Engine.Advising;

/// <summary>
/// Builds and represents the list of properties of a <see cref="ObjectReader"/>.
/// </summary>
internal sealed class ObjectReaderTypeAdapter
{
    private readonly ImmutableDictionary<string, Func<object, object?>> _properties;

    internal ObjectReaderTypeAdapter( Type type )
    {
        Dictionary<string, Func<object, object?>> properties = new();

        if ( type.IsInterface )
        {
            throw new ArgumentOutOfRangeException( nameof(type), "The type cannot be an interface." );
        }

        foreach ( var property in type.GetProperties( BindingFlags.Public | BindingFlags.Instance ) )
        {
            var getter = property.GetMethod;

            if ( getter == null || getter.GetParameters().Length != 0 || getter.ReturnType.IsByRef )
            {
                continue;
            }

            properties[property.Name] = CreateCompiledGetter( property );
        }

        foreach ( var field in type.GetFields( BindingFlags.Public | BindingFlags.Instance ) )
        {
            if ( field.FieldType.IsByRef )
            {
                continue;
            }

            properties[field.Name] = CreateCompiledGetter( field );
        }

        this._properties = properties.ToImmutableDictionary();
    }

    public IEnumerable<string> Properties => this._properties.Keys;

    public int PropertyCount => this._properties.Count;

    public bool TryGetValue( in ProjectServiceProvider serviceProvider, string key, object obj, out object? value )
    {
        if ( !this._properties.TryGetValue( key, out var property ) )
        {
            value = null;

            return false;
        }

        var invoker = serviceProvider.GetRequiredService<UserCodeInvoker>();

        value = invoker.Invoke(
            () => property( obj ),
            new UserCodeExecutionContext(
                serviceProvider,
                UserCodeDescription.Create( "evaluating the {0} field or property", key ) ) );

        return true;
    }

    private static Func<object, object?> CreateCompiledGetter( MemberInfo member )
    {
        var parameter = Expression.Parameter( typeof(object) );
        var castParameter = Expression.Convert( parameter, member.DeclaringType! );
        var getProperty = Expression.Convert( Expression.PropertyOrField( castParameter, member.Name ), typeof(object) );
        var lambda = Expression.Lambda<Func<object, object?>>( getProperty, parameter ).Compile();

        return lambda;
    }

    public bool ContainsProperty( string key ) => this._properties.ContainsKey( key );
}