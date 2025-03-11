// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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