// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using System;
using System.Linq;

// ReSharper disable UnusedType.Local
namespace Metalama.Patterns.Immutability.AspectTests.AttributeAndWarnings.SystemTypeClassificationTests;

// <target>
public class C
{
    private class Fabric : TypeFabric
    {
        [Introduce]
        public static void PrintImmutability()
        {
            var byteType = TypeFactory.GetType( typeof(byte) );

            var typesToCheck = new IType[]
            {
                TypeFactory.GetType( typeof(ValueTuple<int, string>) ),
                TypeFactory.GetType( typeof(DateTime) ),
                TypeFactory.GetType( typeof(Memory<byte>) ),
                TypeFactory.GetType( typeof(ReadOnlyMemory<byte>) ),
                TypeFactory.GetType( "System.Span`1" ).WithTypeArguments( byteType ),
                TypeFactory.GetType( "System.ReadOnlySpan`1" ).WithTypeArguments( byteType )
            };

            foreach ( var type in typesToCheck.OrderBy( t => t.ToString() ) )
            {
                meta.InsertComment( $"{type}: {type.GetImmutabilityKind()}" );
            }
        }
    }
}
