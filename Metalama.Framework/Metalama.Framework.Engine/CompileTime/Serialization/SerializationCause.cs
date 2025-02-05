// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Linq;

namespace Metalama.Framework.Engine.CompileTime.Serialization
{
    internal sealed class SerializationCause
    {
        public SerializationCause? Parent { get; }

        public string Description { get; }

        private SerializationCause( string description, SerializationCause? parent )
        {
            this.Parent = parent;
            this.Description = description;
        }

        public static SerializationCause Root( Type type ) => new( type.Name, null );

        public SerializationCause WithFieldAccess( string fieldName ) => new( "." + fieldName, this );

        public SerializationCause WithArrayAccess( params int[] indices )
        {
            return new SerializationCause( string.Join( "", indices.SelectAsImmutableArray( i => $"[{i}]" ) ), this );
        }
    }
}