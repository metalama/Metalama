// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_OverrideMethod;

internal class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"Override '{meta.Target.Method}'." );

        if ( !meta.Target.Method.IsStatic || (meta.Target.Method.Parameters.Count > 0 && meta.Target.Parameters[0].IsThis) )
        {
            Console.WriteLine( ExpressionFactory.Receiver().Value );
        }

        return meta.Proceed();
    }
}

// <target>
internal static class C
{
    [TheAspect]
    public static void ClassicStaticExtensionMethod( this TestClass c )
    {
        Console.WriteLine( "Original" );
    }

    extension( TestClass test )
    {
        [TheAspect]
        public void Method()
        {
            Console.WriteLine( "Original." );
        }

        [TheAspect]
        public static void StaticMethod()
        {
            Console.WriteLine( "Original." );
        }
    }
}

// <target>
internal class Test
{
    public void Foo()
    {
        var test = new TestClass();
        test.Method();
        TestClass.StaticMethod();
        test.ClassicStaticExtensionMethod();
    }
}

internal class TestClass { }

#endif