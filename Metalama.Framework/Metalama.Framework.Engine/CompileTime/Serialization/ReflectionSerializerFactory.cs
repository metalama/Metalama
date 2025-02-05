// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Serialization;
using System;
using System.Globalization;

namespace Metalama.Framework.Engine.CompileTime.Serialization;

internal sealed class ReflectionSerializerFactory : ISerializerFactory
{
    private readonly UserCodeInvoker _userCodeInvoker;
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly Type _serializerType;

    public ReflectionSerializerFactory( in ProjectServiceProvider serviceProvider, Type serializerType )
    {
        this._userCodeInvoker = serviceProvider.GetRequiredService<UserCodeInvoker>();
        this._serviceProvider = serviceProvider;
        this._serializerType = serializerType;
    }

    public ISerializer CreateSerializer( Type objectType )
    {
        Type serializerTypeInstance;

        if ( objectType.IsGenericTypeDefinition )
        {
            throw new ArgumentOutOfRangeException( nameof(objectType) );
        }

        if ( this._serializerType.IsGenericTypeDefinition )
        {
            if ( this._serializerType.GetGenericArguments().Length == objectType.GetGenericArguments().Length )
            {
                serializerTypeInstance = this._serializerType.MakeGenericType( objectType.GetGenericArguments() );
            }
            else
            {
                throw new CompileTimeSerializationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The serializer type '{0}' is an open generic type, but it has a different number of generic parameters than '{1}'.",
                        this._serializerType,
                        objectType ) );
            }
        }
        else
        {
            serializerTypeInstance = this._serializerType;
        }

        var userCodeExecutionContext = new UserCodeExecutionContext(
            this._serviceProvider,
            UserCodeDescription.Create( "instantiating the serializer type", serializerTypeInstance ) );

        var instance = this._userCodeInvoker.Invoke( () => Activator.CreateInstance( serializerTypeInstance ), userCodeExecutionContext );

        return instance switch
        {
            ISerializer serializer => serializer,
            ISerializerFactory serializerFactory => serializerFactory.CreateSerializer( objectType ),
            _ => throw new CompileTimeSerializationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Type '{0}' must implement interface ISerializer or ISerializerFactory.",
                    serializerTypeInstance ) )
        };
    }
}