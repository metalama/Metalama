// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.References;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime.Serialization;

internal sealed class DeserializationSurrogateProvider : IDeserializationSurrogateProvider
{
    public bool TryGetDeserializationSurrogate( string typeName, [NotNullWhen( true )] out Type? surrogateType )
    {
        if ( typeName is "Metalama.Framework.Engine.CodeModel.References.Ref`1" or "Metalama.Framework.Engine.CodeModel.References.BoxedRef`1" )
        {
            surrogateType = typeof(DeclarationIdRef<>);

            return true;
        }
        else
        {
            surrogateType = null;

            return false;
        }
    }
}