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

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_Contract_OnReceiver_Operator;

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

internal class TestClass
{
    public int Value { get; set; }
}

// <target>
[MyTypeAspect]
internal static class C
{
    extension( TestClass test )
    {
        // Instance method - should have contract
        public void InstanceMethod()
        {
            Console.WriteLine( "Instance method." );
        }

        // Operators are static - should NOT have contract
        public static TestClass operator +( TestClass a, TestClass b )
        {
            return new TestClass { Value = a.Value + b.Value };
        }

        public static TestClass operator -( TestClass a, TestClass b )
        {
            return new TestClass { Value = a.Value - b.Value };
        }

        public static TestClass operator *( TestClass a, int scalar )
        {
            return new TestClass { Value = a.Value * scalar };
        }

        // Unary operators
        public static TestClass operator ++( TestClass a )
        {
            return new TestClass { Value = a.Value + 1 };
        }

        public static TestClass operator --( TestClass a )
        {
            return new TestClass { Value = a.Value - 1 };
        }

        public static bool operator ==( TestClass a, TestClass b )
        {
            return a.Value == b.Value;
        }

        public static bool operator !=( TestClass a, TestClass b )
        {
            return a.Value != b.Value;
        }
    }
}
#endif
