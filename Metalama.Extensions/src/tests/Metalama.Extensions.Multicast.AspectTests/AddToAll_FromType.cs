// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @Include(_Tagging.cs)
#endif

// ReSharper disable EventNeverSubscribedTo.Global, UnusedParameter.Global

namespace Metalama.Extensions.Multicast.AspectTests.AddToAllFromType
{
    // <target>
    [AddTag( "Tagged" )]
    public class AClass
    {
        public int Method( int p ) => 0;

        public int Property { get; private set; }

        public int Field;

        public event Action PublicEvent;

        // Nested classes are ignored for PostSharp compatibility.
        private class Nested
        {
            private void Method() { }
        }
    }
}