// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.SerializableIds;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;

namespace Metalama.Framework.Engine.CodeModel.Factories
{
    /// <summary>
    /// Creates and ensures uniqueness of instances of the <see cref="CompileTimeType"/> class.
    /// </summary>
    internal class CompileTimeTypeFactory
    {
        // The class is intentionally project-scoped even if does not depend on the project because
        // we want the lifetime and scope of this dictionary to be project-scoped.
        // Key is SerializableTypeId for most types. Only type parameters can't be represented using just that, so they use SymbolId.
        private readonly ConcurrentDictionary<string, CompileTimeType> _instances = new( StringComparer.Ordinal );

        public CompileTimeType Get( ITypeSymbol symbol )
            => symbol switch
            {
                IDynamicTypeSymbol => throw new AssertionFailedException( "Cannot get a System.Type for the 'dynamic' type." ),
                IArrayTypeSymbol { ElementType: IDynamicTypeSymbol } => throw new AssertionFailedException(
                    "Cannot get a System.Type for the 'dynamic[]' type." ),
                _ => this.Get( symbol.GetSerializableTypeId( true ).Id, symbol )
            };
        
        // TODO: Backward compatibility with SymbolId-backed types.

        private CompileTimeType Get( string id, ITypeSymbol symbolForMetadata )
        {
            return this._instances.GetOrAdd(
                id,
                static ( key, x ) =>
                    CompileTimeType.CreateFromTypeId( new SerializableTypeId( key ), x.symbolForMetadata ),
                (me: this, symbolForMetadata) );
        }

        public CompileTimeType Get( SerializableTypeId declarationId, CompileTimeTypeMetadata metadata )
        {
            return this._instances.GetOrAdd(
                declarationId.ToString(),
                static ( id, x ) => CompileTimeType.CreateFromTypeId( new SerializableTypeId( id ), x.metadata ),
                (me: this, metadata) );
        }
    }
}