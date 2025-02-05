// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @Include(_Tagging.cs)
#endif

using Metalama.Extensions.Multicast.AspectTests;

[assembly: AddTag( "Tagged" )]

// ReSharper disable EventNeverSubscribedTo.Global, UnusedParameter.Global

// <target>
namespace Metalama.Extensions.Multicast.AspectTests.AddToAllFromCompilation
{
    public class AClass
    {
        public int Method( int p ) => 0;

        public int Property { get; private set; }

        public int Field;

        public event Action PublicEvent;
    }

    public class AStruct
    {
        private class Nested
        {
            public int Method( int p ) => 0;

            public int Property { get; private set; }

            public int Field;

            public event Action PublicEvent;
        }
    }

    public enum AnEnum { }

    public delegate void ADelegate();
}