// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Workspaces.Navigation.Facade;

/// <summary>
/// Builds and represents the list of properties of an object that tools display.
/// </summary>
/// <remarks>
/// Properties of public interfaces from <see cref="FacadeTypeFactory.PublicAssemblies"/> are shown, then public properties and
/// fields of the nearest public base type. Members and interfaces marked with <see cref="HiddenAttribute"/>, and properties
/// marked with <see cref="ObsoleteAttribute"/>, are skipped.
/// </remarks>
internal sealed class FacadeType
{
    private readonly FacadeTypeFactory _factory;
    private readonly ImmutableDictionary<string, FacadeProperty> _propertiesByName;

    public IEnumerable<string> PropertyNames { get; }

    public IEnumerable<Type> PropertyTypes { get; }

    public ImmutableArray<FacadeProperty> Properties { get; }

    internal FacadeType( FacadeTypeFactory factory, Type type )
    {
        this._factory = factory;

        Dictionary<string, FacadeProperty> properties = new();

        if ( type.IsInterface )
        {
            throw new ArgumentOutOfRangeException( nameof(type), "The type cannot be an interface." );
        }

        // Find getters of properties of public interfaces.
        var implementedInterfaces = type.GetInterfaces();

        foreach ( var implementedInterface in implementedInterfaces )
        {
            if ( !this.IsPublicType( implementedInterface ) )
            {
                continue;
            }

            var interfaceMap = type.GetInterfaceMap( implementedInterface );

            for ( var i = 0; i < interfaceMap.TargetMethods.Length; i++ )
            {
                // We need to take the interface method, and not the implementation method, because the implementation may have been obfuscated
                // in the Release build.

                var publicGetter = interfaceMap.InterfaceMethods[i];

                if ( publicGetter.Name.StartsWith( "get_", StringComparison.Ordinal ) && publicGetter.GetParameters().Length == 0 )
                {
                    var propertyName = publicGetter.Name.Substring( 4 );

                    if ( properties.ContainsKey( propertyName ) )
                    {
                        // The property was defined in the parent interface.
                        continue;
                    }

                    var property =
                        publicGetter.DeclaringType!.GetProperty(
                            propertyName,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );

                    // Code model objects are internal classes reached through their public interfaces, so the attributes
                    // must be checked on the interface property and not only on the properties of a public base class.
                    if ( property != null && !IsHiddenProperty( property ) )
                    {
                        properties[propertyName] = new FacadeProperty( propertyName, publicGetter.ReturnType, CreateCompiledGetter( publicGetter ) );
                    }
                }
            }
        }

        // Find getters of public properties.
        var publicType = this.GetPublicBase( type );

        if ( publicType == null && implementedInterfaces.Length == 0 )
        {
            // When the type is completely internal, like anonymous types, expose it anyway.
            publicType = type;
        }

        if ( publicType != null )
        {
            foreach ( var property in publicType.GetProperties( BindingFlags.Public | BindingFlags.Instance ) )
            {
                if ( IsHiddenProperty( property ) )
                {
                    continue;
                }

                var getter = property.GetMethod;

                if ( getter == null || getter.GetParameters().Length != 0 )
                {
                    continue;
                }

                properties[property.Name] = new FacadeProperty( property.Name, property.PropertyType, CreateCompiledGetter( property ) );
            }

            foreach ( var field in publicType.GetFields( BindingFlags.Public | BindingFlags.Instance ) )
            {
                if ( field.IsDefined( typeof(HiddenAttribute) ) )
                {
                    continue;
                }

                properties[field.Name] = new FacadeProperty( field.Name, field.FieldType, CreateCompiledGetter( field ) );
            }
        }

        // A projection to an anonymous type keeps the order in which the query wrote its members.
        var facadeProperties = IsAnonymousType( type )
            ? properties.Values.ToList()
            : properties.Values.ToOrderedList( p => (p.Name, p.Type), PropertyComparer.Instance );

        this.Properties = facadeProperties.ToImmutableArray();
        this._propertiesByName = this.Properties.ToImmutableDictionary( p => p.Name, StringComparer.Ordinal );

        this.PropertyNames = this.Properties.Select( p => p.Name ).ToImmutableArray();
        this.PropertyTypes = this.Properties.Select( p => p.Type ).ToImmutableArray();
    }

    /// <summary>
    /// Gets a displayed property by name. Hidden properties are not found.
    /// </summary>
    public bool TryGetProperty( string name, out FacadeProperty property )
    {
        if ( this._propertiesByName.TryGetValue( name, out var found ) )
        {
            property = found;

            return true;
        }

        property = null!;

        return false;
    }

    internal static bool IsAnonymousType( Type type )
        => type.IsDefined( typeof(CompilerGeneratedAttribute), false ) && type.Name.Contains( "AnonymousType", StringComparison.Ordinal );

    private static bool IsHiddenProperty( PropertyInfo property )
        => property.IsDefined( typeof(HiddenAttribute) ) || property.IsDefined( typeof(ObsoleteAttribute) );

    private static Func<object, object?> CreateCompiledGetter( MemberInfo member )
    {
        var parameter = Expression.Parameter( typeof(object) );
        var castParameter = Expression.Convert( parameter, member.DeclaringType! );
        var getProperty = Expression.Convert( Expression.PropertyOrField( castParameter, member.Name ), typeof(object) );
        var lambda = Expression.Lambda<Func<object, object?>>( getProperty, parameter ).Compile();

        return lambda;
    }

    private static Func<object, object?> CreateCompiledGetter( MethodInfo getter )
    {
        var parameter = Expression.Parameter( typeof(object) );
        var castParameter = Expression.Convert( parameter, getter.DeclaringType! );
        var getProperty = Expression.Convert( Expression.Call( castParameter, getter ), typeof(object) );
        var lambda = Expression.Lambda<Func<object, object?>>( getProperty, parameter ).Compile();

        return lambda;
    }

    private bool IsPublicAssembly( Assembly assembly ) => this._factory.PublicAssemblies.Contains( assembly );

    private bool IsPublicType( Type type )
    {
        if ( !type.IsPublic && type.Assembly != typeof(FacadeType).Assembly )
        {
            return false;
        }

        if ( !this.IsPublicAssembly( type.Assembly ) )
        {
            return false;
        }

        var attribute = type.GetCustomAttribute<HiddenAttribute>();

        if ( attribute != null )
        {
            return false;
        }

        return true;
    }

    private Type? GetPublicBase( Type type )
        => this.IsPublicType( type ) ? type : type.BaseType != null && type.BaseType != typeof(object) ? this.GetPublicBase( type.BaseType ) : null;
}
