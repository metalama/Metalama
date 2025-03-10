// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS0067, CS0414

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.Struct_Declarative
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public event EventHandler? IntroducedEvent;

        [Introduce]
        public event EventHandler? IntroducedEvent_Initializer = Foo;

        [Introduce]
        public event EventHandler? IntroducedEvent_Static;

        [Introduce]
        public event EventHandler? IntroducedEvent_Static_Initializer = Foo;

        [Introduce]
        public static void Foo( object? sender, EventArgs args ) { }
    }

    // <target>
    [Introduction]
    internal struct TargetStruct
    {
        private int _existingField;

        public TargetStruct( int x )
        {
            _existingField = x;
        }
    }
}