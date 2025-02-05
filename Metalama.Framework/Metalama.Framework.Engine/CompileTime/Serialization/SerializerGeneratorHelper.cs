// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Serialization;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Engine.CompileTime.Serialization;

internal static class SerializerGeneratorHelper
{
    internal static bool TryGetSerializer(
        CompilationContext compilationContext,
        INamedTypeSymbol type,
        [NotNullWhen( true )] out INamedTypeSymbol? serializerType,
        out bool ambiguous )
    {
        var serializers =
            type.GetTypeMembers()
                .Where(
                    bt =>
                        bt.AllInterfaces.Contains(
                            compilationContext.ReflectionMapper.GetTypeSymbol( typeof(ISerializer) ),
                            compilationContext.SymbolComparer ) )
                .ToImmutableArray();

        switch ( serializers.Length )
        {
            case 0:
                serializerType = null;
                ambiguous = false;

                return false;

            case > 1:
                serializerType = null;
                ambiguous = true;

                return false;

            default:
                serializerType = serializers[0];
                ambiguous = false;

                return true;
        }
    }
}