#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER) - Default interface members need to be supported by the runtime.
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.InterfaceDefaultMember
{
    /*
     * Tests overriding of default interface implementation events.
     */

    internal class OverrideAttribute : OverrideEventAspect
    {
        public override void OverrideAdd( dynamic value )
        {
            Console.WriteLine( "Override." );
            meta.Proceed();
        }

        public override void OverrideRemove( dynamic value )
        {
            Console.WriteLine( "Override." );
            meta.Proceed();
        }
    }

    public interface InterfaceA
    {
        event EventHandler? EventA;
    }

    // <target>
    public interface InterfaceB : InterfaceA
    {
#if NET5_0_OR_GREATER
        [Override]
        event EventHandler? InterfaceA.EventA
        {
            add
            {
                Console.WriteLine("Default implementation");
            }

            remove
            {
                Console.WriteLine("Default implementation");
            }
        }

        [Override]
        event EventHandler? EventB
        {
            add
            {
                Console.WriteLine("Default implementation");
            }

            remove
            {
                Console.WriteLine("Default implementation");
            }
        }
#endif
    }

    // <target>
    public class TargetClass : InterfaceB
    {
#if !NET5_0_OR_GREATER
        public event EventHandler? EventA;
#endif
    }
}