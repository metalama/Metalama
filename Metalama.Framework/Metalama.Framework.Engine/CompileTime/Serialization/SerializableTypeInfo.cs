// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CompileTime.Serialization
{
    /// <summary>
    /// Describes serializable type, serializer of which needs to be emitted in this compilation.
    /// </summary>
    internal sealed class SerializableTypeInfo
    {
        /// <summary>
        /// Gets the serializable type.
        /// </summary>
        public INamedTypeSymbol Type { get; }

        /// <summary>
        /// Gets a list of serializable fields an properties.
        /// </summary>
        public List<ISymbol> SerializedMembers { get; } = new();

        public SerializableTypeInfo( INamedTypeSymbol type, IReadOnlyList<ISymbol> serializedMembers )
        {
            this.Type = type;
            this.SerializedMembers.AddRange( serializedMembers );
        }
    }
}