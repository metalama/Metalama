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

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_Contract_OnReceiver_Multiple;

internal class MyTypeAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var extensionBlock in builder.Target.ExtensionBlocks )
        {
            // Add two contracts to the same receiver parameter
            builder.With( extensionBlock.ReceiverParameter ).AddContract( nameof(ValidateNotNull) );
            builder.With( extensionBlock.ReceiverParameter ).AddContract( nameof(ValidateLength) );
        }
    }

    [Template]
    private void ValidateNotNull( dynamic? value )
    {
        if ( value == null )
        {
            throw new ArgumentNullException();
        }
        Console.WriteLine( $"NotNull contract passed: {value}" );
    }

    [Template]
    private void ValidateLength( dynamic? value )
    {
        if ( ((string)value!).Length == 0 )
        {
            throw new ArgumentException( "String cannot be empty." );
        }
        Console.WriteLine( $"Length contract passed: {value}" );
    }
}

// <target>
[MyTypeAspect]
internal static class C
{
    extension( string test )
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
