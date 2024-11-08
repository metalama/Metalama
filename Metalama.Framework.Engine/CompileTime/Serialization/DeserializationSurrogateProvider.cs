// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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