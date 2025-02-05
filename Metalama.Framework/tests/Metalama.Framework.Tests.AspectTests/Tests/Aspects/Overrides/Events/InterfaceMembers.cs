#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER) - Default interface members need to be supported by the runtime.
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.InterfaceMembers
{
    /*
     * Tests overriding of interface non-abstract events.
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

    // <target>
    public interface Interface
    {
#if NET5_0_OR_GREATER
        [Override]
        private event EventHandler PrivateEvent
        {
            add
            {
                Console.WriteLine("Original implementation");
            }

            remove
            {
                Console.WriteLine("Original implementation");
            }
        }

        [Override]
        public static event EventHandler StaticEvent
        {
            add
            {
                Console.WriteLine("Original implementation");
            }

            remove
            {
                Console.WriteLine("Original implementation");
            }
        }
#endif
    }

    // <target>
    public class TargetClass : Interface { }
}