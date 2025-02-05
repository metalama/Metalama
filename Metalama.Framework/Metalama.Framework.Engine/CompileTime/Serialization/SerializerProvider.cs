// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Metalama.Framework.Engine.CompileTime.Serialization
{
    internal sealed class SerializerProvider
    {
        private readonly ISerializerFactoryProvider _provider;
        private readonly SerializerProvider? _next;
        private readonly Dictionary<Type, ISerializer> _serializers = new( 64 );
        private readonly object _sync = new();

        public SerializerProvider( ISerializerFactoryProvider provider )
        {
            this._provider = provider;

            if ( provider.NextProvider != null )
            {
                this._next = new SerializerProvider( provider.NextProvider );
            }
        }

        private void DiscoverSerializers( Type objectType )
        {
            for ( var currentProvider = this._provider; currentProvider != null; currentProvider = currentProvider.NextProvider )
            {
                if ( currentProvider is ISerializerDiscoverer serializerDiscoverer )
                {
                    serializerDiscoverer.DiscoverSerializers( objectType );
                }
            }
        }

        public ISerializer GetSerializer( Type objectType, SerializationCause? cause )
        {
            if ( !this.TryGetSerializer( objectType, out var serializer ) )
            {
                throw CompileTimeSerializationException.CreateWithCause(
                    string.Format( CultureInfo.InvariantCulture, "The type '{0}' is not serializable.", objectType ),
                    cause );
            }

            return serializer.AssertNotNull();
        }

        public bool TryGetSerializer( Type objectType, out ISerializer? serializer )
        {
            if ( objectType.HasElementType )
            {
                throw new ArgumentOutOfRangeException( nameof(objectType) );
            }

            lock ( this._sync )
            {
                if ( this._serializers.TryGetValue( objectType, out serializer ) )
                {
                    return true;
                }

                this.DiscoverSerializers( objectType );

                var serializerFactory = this._provider.GetSerializerFactory( objectType );

                if ( serializerFactory == null )
                {
                    if ( objectType.IsGenericType )
                    {
                        serializerFactory = this._provider.GetSerializerFactory( objectType.GetGenericTypeDefinition() );
                    }

                    if ( serializerFactory == null )
                    {
                        if ( this._next != null )
                        {
                            return this._next.TryGetSerializer( objectType, out serializer );
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                serializer = serializerFactory.CreateSerializer( objectType );

                this._serializers.Add( objectType, serializer );

                return true;
            }
        }
    }
}