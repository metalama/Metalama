// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Serialization;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CompileTime.Serialization;

/// <summary>
/// Provides instances of the <see cref="ISerializerFactory"/> interface for object types that have been previously registered
/// using <see cref="AddSerializer"/>.
/// </summary>
internal class SerializerFactoryProvider : ISerializerFactoryProvider
{
    private readonly Dictionary<Type, ISerializerFactory> _serializerTypes = new( 64 );
    private readonly ProjectServiceProvider _serviceProvider;

    private bool _isReadOnly;

    /// <inheritdoc />
    public ISerializerFactoryProvider? NextProvider { get; }

    /// <summary>
    /// Forbids further changes in the current <see cref="SerializerFactoryProvider"/>.
    /// </summary>
    protected void MakeReadOnly()
    {
        if ( this._isReadOnly )
        {
            throw new InvalidOperationException();
        }

        this._isReadOnly = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializerFactoryProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="nextProvider">The next provider in the chain, or <c>null</c> if there is none.</param>
    protected SerializerFactoryProvider( in ProjectServiceProvider serviceProvider, ISerializerFactoryProvider nextProvider )
    {
        this._serviceProvider = serviceProvider;
        this.NextProvider = nextProvider;
    }

    /// <summary>
    /// Maps an object type to a serializer type (using generic type parameters).
    /// </summary>
    /// <typeparam name="TObject">Type of the serialized object.</typeparam>
    /// <typeparam name="TSerializer">Type of the serializer.</typeparam>
    protected void AddSerializer<TObject, TSerializer>()
        where TSerializer : ISerializer, new()
        => this.AddSerializer( typeof(TObject), typeof(TSerializer) );

    /// <summary>
    /// Maps an object type to a serializer type.
    /// </summary>
    /// <param name="objectType">Type of the serialized object.</param>
    /// <param name="serializerType">Type of the serializer (must be derived from <see cref="ISerializer"/>).</param>
    protected void AddSerializer( Type objectType, Type serializerType )
    {
        if ( this._isReadOnly )
        {
            throw new InvalidOperationException();
        }

        if ( !typeof(ISerializer).IsAssignableFrom( serializerType ) )
        {
            throw new ArgumentOutOfRangeException( nameof(serializerType), "Type '{0}' does not implement ISerializer or IGenericSerializerFactory" );
        }

        this._serializerTypes.Add( objectType, new ReflectionSerializerFactory( this._serviceProvider, serializerType ) );
    }

    /// <inheritdoc />
    public virtual ISerializerFactory? GetSerializerFactory( Type objectType )
    {
        if ( this._serializerTypes.TryGetValue( objectType, out var serializerType ) )
        {
            return serializerType;
        }
        else if ( this.NextProvider != null )
        {
            return this.NextProvider.GetSerializerFactory( objectType );
        }
        else
        {
            return null;
        }
    }
}