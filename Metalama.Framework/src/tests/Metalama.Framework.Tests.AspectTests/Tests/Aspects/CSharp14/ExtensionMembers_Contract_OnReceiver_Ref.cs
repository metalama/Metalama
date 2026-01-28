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

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_Contract_OnReceiver_Ref;

internal class MyTypeAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var extensionBlock in builder.Target.ExtensionBlocks )
        {
            // For ref receivers, add both input and output contracts
            builder.With( extensionBlock.ReceiverParameter ).AddContract( nameof(ValidateTemplate), ContractDirection.Both );
        }
    }

    [Template]
    private void ValidateTemplate( dynamic? value )
    {
        Console.WriteLine( $"Contract on receiver: {value}" );
    }
}

internal struct MyStruct
{
    public int Value;
}

// <target>
[MyTypeAspect]
internal static class C
{
    extension( ref MyStruct test )
    {
        public void Method()
        {
            Console.WriteLine( "Method." );
        }

        public int Property
        {
            get
            {
                Console.WriteLine( "Property get." );
                return 42;
            }
            set
            {
                Console.WriteLine( "Property set." );
            }
        }
    }
}
#endif
