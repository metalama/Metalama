#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER) - Default interface members need to be supported by the runtime.
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.InterfaceDefaultMember
{
    /*
     * Tests overriding of default interface implementation methods.
     */

    internal class OverrideAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "Override." );

            return meta.Proceed();
        }
    }

    public interface InterfaceA
    {
        int MethodA();
    }

    // <target>
    public interface InterfaceB : InterfaceA
    {
#if NET5_0_OR_GREATER
        [Override]
        int InterfaceA.MethodA()
        {
            Console.WriteLine("Default implementation");
            return 42;
        }

        [Override]
        int MethodB()
        {
            Console.WriteLine("Default implementation");
            return 42;
        }
#endif
    }

    // <target>
    public class TargetClass : InterfaceB
    {
#if !NET5_0_OR_GREATER
        public int MethodA()
        {
            return -1;
        }
#endif
    }
}