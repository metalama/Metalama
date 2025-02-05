// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @Include(_Tagging.cs)
#endif

// ReSharper disable EventNeverSubscribedTo.Global, UnusedParameter.Global

namespace Metalama.Extensions.Multicast.AspectTests.AddToAll_FromMembers
{
    // <target>
    public class AClass
    {
        [AddTag( "Tagged" )]
        public int Method( int p ) => 0;

        [AddTag( "Tagged" )]
        public int Property { get; private set; }

        public int Field;

        [AddTag( "Tagged" )]
        public event Action PublicEvent { add { } remove { } }
    }
}