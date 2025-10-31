// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_Contract;

internal class TheAspect : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        Console.WriteLine( $"Member: {meta.Target.Member}" );
        Console.WriteLine( $"Type: {meta.Target.Type}" );

        if ( meta.Target.Method.HasReceiver() )
        {
            Console.WriteLine( meta.Receiver );
        }
    }

}

// <target>
internal static class C
{
    public static void ClassicStaticExtensionMethod( [TheAspect] this TestClass c )
    {
        Console.WriteLine( "Original" );
    }

    extension( TestClass test )
    {
        public static TestClass operator *( [TheAspect] TestClass vector, float scalar ) => vector;
        
        
        public void Method([TheAspect] int x)
        {
            Console.WriteLine( "Original." );
        }

        public static void StaticMethod([TheAspect] int x)
        {
            Console.WriteLine( "Original." );
        }
        
        [TheAspect]
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

        [TheAspect]
        public static int StaticProperty
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

// <target>
internal class Test
{
    public void Foo()
    {
        var test = new TestClass();
        test.Method(1);
        TestClass.StaticMethod(1);
        test.ClassicStaticExtensionMethod();
        _ = test * 5;
        test *= 10;
    }
}

internal class TestClass { }

#endif