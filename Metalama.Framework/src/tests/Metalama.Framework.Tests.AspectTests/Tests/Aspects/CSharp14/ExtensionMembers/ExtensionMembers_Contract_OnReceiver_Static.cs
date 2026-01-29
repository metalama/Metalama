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

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_Contract_OnReceiver_Static;

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
        Console.WriteLine( $"Contract on receiver: {value}" );
    }
}

// <target>
[MyTypeAspect]
internal static class C
{
    extension( string test )
    {
        // Instance method - should have contract
        public void InstanceMethod()
        {
            Console.WriteLine( "Instance method." );
        }

        // Static method - should NOT have contract (no access to receiver)
        public static void StaticMethod()
        {
            Console.WriteLine( "Static method." );
        }

        // Instance property - should have contract
        public int InstanceProperty
        {
            get
            {
                Console.WriteLine( "Instance property get." );
                return 42;
            }
            set
            {
                Console.WriteLine( "Instance property set." );
            }
        }

        // Static property - should NOT have contract
        public static int StaticProperty
        {
            get
            {
                Console.WriteLine( "Static property get." );
                return 42;
            }
            set
            {
                Console.WriteLine( "Static property set." );
            }
        }
    }
}
#endif
