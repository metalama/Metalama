// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable CS0067

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Introductions.Events.DeclarativeEvent
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public event EventHandler? Event
        {
            add
            {
                Console.WriteLine( "Original add accessor." );
            }

            remove
            {
                Console.WriteLine( "Original remove accessor." );
            }
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}