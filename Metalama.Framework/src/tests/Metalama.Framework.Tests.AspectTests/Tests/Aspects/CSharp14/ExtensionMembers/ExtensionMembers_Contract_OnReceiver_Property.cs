// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_Contract_OnReceiver_Property;

internal class MyTypeAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var extensionBlock in builder.Target.ExtensionBlocks )
        {
            builder.With( extensionBlock.ReceiverParameter ).AddContract( nameof(ValidateTemplate) );
        }
    }

    [Template]
    private void ValidateTemplate( dynamic? value )
    {
        Console.WriteLine( $"Contract on receiver: {value}, Member: {meta.Target.Member}" );
    }
}

// <target>
[MyTypeAspect]
internal static class C
{
    extension( string test )
    {
        // Property with both getter and setter
        public int ReadWriteProperty
        {
            get
            {
                Console.WriteLine( "ReadWriteProperty get." );
                return 42;
            }
            set
            {
                Console.WriteLine( $"ReadWriteProperty set: {value}" );
            }
        }

        // Property with only getter (computed property)
        public int ReadOnlyProperty
        {
            get
            {
                Console.WriteLine( "ReadOnlyProperty get." );
                return 42;
            }
        }

        // Property with only setter
        public int WriteOnlyProperty
        {
            set
            {
                Console.WriteLine( $"WriteOnlyProperty set: {value}" );
            }
        }

    }
}
#endif
