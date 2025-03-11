// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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