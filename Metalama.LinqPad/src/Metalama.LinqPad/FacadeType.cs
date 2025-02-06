// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Metalama.LinqPad
{
    /// <summary>
    /// Builds and represents the list of properties of a <see cref="FacadeObject"/>.
    /// </summary>
    internal sealed class FacadeType
    {
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly FacadeObjectFactory _factory;

        public IEnumerable<string> PropertyNames { get; }

        public IEnumerable<Type> PropertyTypes { get; }

        public ImmutableArray<FacadeProperty> Properties { get; }

        internal FacadeType( FacadeObjectFactory factory, Type type )
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

                        if ( property != null )
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
                    if ( property.GetCustomAttributes<HiddenAttribute>().Any() ||
                         property.GetCustomAttributes<ObsoleteAttribute>().Any() )
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
                    if ( field.GetCustomAttributes<HiddenAttribute>().Any() )
                    {
                        continue;
                    }

                    properties[field.Name] = new FacadeProperty( field.Name, field.FieldType, CreateCompiledGetter( field ) );
                }
            }

            var facadeProperties = properties.Values
                .ToOrderedList( p => (p.Name, p.Type), PropertyComparer.Instance );

            if ( typeof(IDeclaration).IsAssignableFrom( type ) )
            {
                facadeProperties.Add(
                    new FacadeProperty(
                        "Permalink",
                        typeof(Permalink),
                        o =>
                        {
                            var declaration = (IDeclaration) o;

                            var getCompilationInfo = this._factory.GetGetCompilationInfo( declaration );

                            return new Permalink( getCompilationInfo, declaration );
                        } ) );
            }

            this.Properties = facadeProperties.ToImmutableArray();

            this.PropertyNames = this.Properties.Select( p => p.Name ).ToImmutableArray();
            this.PropertyTypes = this.Properties.Select( p => p.Type ).ToImmutableArray();
        }

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
}