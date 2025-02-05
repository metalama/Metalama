// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Metalama.Framework.Engine.CompileTime.Serialization;

internal sealed class ReflectionSerializationProvider : ISerializerFactoryProvider, ISerializerDiscoverer
{
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly Dictionary<Type, ISerializerFactory> _serializerTypes = new();
    private readonly Dictionary<Type, bool> _inspectedTypes = new();
    private readonly Dictionary<Assembly, bool> _inspectedAssemblies = new();
    private readonly object _sync = new();

    public ReflectionSerializationProvider( in ProjectServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
    }

    public ISerializerFactory? GetSerializerFactory( Type objectType )
    {
        // If we have a generic type instance, we return null and wait to be called a second time with the generic type definition.
        if ( objectType is { IsGenericType: true, IsGenericTypeDefinition: false } )
        {
            return null;
        }

        lock ( this._sync )
        {
            this.InspectType( objectType );

            this._serializerTypes.TryGetValue( objectType, out var serializerType );

            return serializerType;
        }
    }

    private void AddSerializer( Type objectType, Type serializerType )
    {
        if ( this._serializerTypes.TryGetValue( objectType, out var existingSerializerType ) )
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if ( ReferenceEquals( existingSerializerType, serializerType ) )
            {
                return;
            }
            else
            {
                throw new CompileTimeSerializationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Cannot assign serializer '{0}' to type '{1}' where this type is already assigned to serializer '{2}'.",
                        serializerType,
                        objectType,
                        existingSerializerType ) );
            }
        }

        this._serializerTypes.Add( objectType, new ReflectionSerializerFactory( this._serviceProvider, serializerType ) );
    }

    private void InspectType( Type type )
    {
        if ( this._inspectedTypes.ContainsKey( type ) )
        {
            return;
        }

        this._inspectedTypes.Add( type, true );

        try
        {
            // Find the serializer defined as a nested type.
            using var serializers = type.GetNestedTypes( BindingFlags.Public | BindingFlags.NonPublic )
                .Where( n => typeof(ISerializer).IsAssignableFrom( n ) )
                .GetEnumerator();

            if ( serializers.MoveNext() )
            {
                this.AddSerializer( type, serializers.Current.AssertNotNull() );

                if ( serializers.MoveNext() )
                {
                    throw new CompileTimeSerializationException( $"The type {type} has more than one serializer." );
                }
            }

            foreach ( var attribute in type.GetCustomAttributes<ImportSerializerAttribute>() )
            {
                this.ProcessImport( attribute );

                break;
            }
        }
        catch ( FormatException )
        {
            // This happens on Windows Phone 7 if the aspect has a custom attribute with a generic type as an argument.
            // In this case, we ignore the exception and look for the Serializer nested type.
        }

        var baseType = type.BaseType;

        if ( baseType != null )
        {
            if ( baseType.IsGenericType )
            {
                baseType = baseType.GetGenericTypeDefinition();
            }

            this.InspectType( baseType );
        }

        this.InspectAssembly( type.Assembly );
    }

    private void InspectAssembly( Assembly assembly )
    {
        if ( this._inspectedAssemblies.ContainsKey( assembly ) )
        {
            return;
        }

        this._inspectedAssemblies.Add( assembly, true );

        foreach ( ImportSerializerAttribute attribute in assembly.GetCustomAttributes( typeof(ImportSerializerAttribute), false ) )
        {
            this.ProcessImport( attribute );
        }
    }

    private void ProcessImport( ImportSerializerAttribute importSerializerAttribute )
    {
        if ( importSerializerAttribute.ObjectType != null! && importSerializerAttribute.SerializerType != null! )
        {
            this.AddSerializer( importSerializerAttribute.ObjectType, importSerializerAttribute.SerializerType );
        }
    }

    public ISerializerFactoryProvider? NextProvider => null;

    public void DiscoverSerializers( Type objectType )
    {
        lock ( this._sync )
        {
            this.InspectType( objectType );
        }
    }
}