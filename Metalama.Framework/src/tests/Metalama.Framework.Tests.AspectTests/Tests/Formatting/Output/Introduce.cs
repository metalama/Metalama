// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618, CS0067, CS0168

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.Introduce
{
    public class IntroduceAspect : TypeAspect
    {
        [Introduce]
        public void Method()
        {
            Console.WriteLine( "Hello, world." );
        }

        [Introduce]
        public int Property { get; set; }

        [Introduce]
        public int PropertyWithBody
        {
            get
            {
                return 1;
            }
            set
            {
                Console.WriteLine( "Set" );
            }
        }

        [Introduce]
        public event EventHandler? Event;
    }

    [IntroduceAspect]
    public class TargetCode { }
}