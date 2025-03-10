// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SerializableIds;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Factories;

internal sealed class ResolvingCompileTimeTypeFactory : CompileTimeTypeFactory
{
    private readonly SerializableTypeIdResolverForSymbol _serializableTypeIdResolver;

    public ResolvingCompileTimeTypeFactory( SerializableTypeIdResolverForSymbol serializableTypeIdResolver ) 
    {
        this._serializableTypeIdResolver = serializableTypeIdResolver;
    }

    public Type Get( SerializableTypeId typeId, IReadOnlyDictionary<string, IType>? substitutions )
        => this.Get( this._serializableTypeIdResolver.ResolveId( typeId, substitutions ) );
}