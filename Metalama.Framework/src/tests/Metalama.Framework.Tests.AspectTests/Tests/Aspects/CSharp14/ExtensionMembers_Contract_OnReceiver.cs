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
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_Contract_OnReceiver;

internal class MyContractAspect : ContractAspect
{
    public override void Validate( dynamic? value )
    {
    }

}

internal class MyTypeAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.ExtensionBlocks.Single( b => b.ReceiverType.Equals( SpecialType.String ) ).ReceiverParameter )
            .AddContract( nameof(ValidateTemplate) );
    }

    [Template]
    private void ValidateTemplate( dynamic? value )
    {
        Console.WriteLine( $"Contract on: {value}" );
    }
}

// <target>
[MyTypeAspect]
internal static class C
{
    extension( [MyContractAspect] int test )
    {
        public int Property
        {
            get
            {
                Console.WriteLine( "Original." );
                return 42;
            }
            set
            {
                Console.WriteLine( "Original." );
            }
        }

    }
    
    extension( string test )
    {
        public int Property
        {
            get
            {
                Console.WriteLine( "Original." );
                return 42;
            }
            set
            {
                Console.WriteLine( "Original." );
            }
        }

    }
}
#endif