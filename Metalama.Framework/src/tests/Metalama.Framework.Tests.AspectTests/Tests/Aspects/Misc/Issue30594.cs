// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

/*
  #30594
  Enums marked as [RunTimeOrCompileTime] are not seen from compile-time assembly when defined in a referenced project with no aspect
*/

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue30594
{
    internal class MyAspect : OverrideMethodAspect
    {
        public MyEnum Property { get; set; }

        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( Property.ToString() );

            return meta.Proceed();
        }
    }

    // <target>
    internal class C
    {
        [MyAspect( Property = MyEnum.MyValue )]
        public void M() { }
    }
}