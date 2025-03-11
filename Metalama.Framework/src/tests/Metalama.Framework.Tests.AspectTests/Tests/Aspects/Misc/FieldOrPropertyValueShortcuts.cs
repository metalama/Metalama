// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS1717, CS0414

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.FieldOrPropertyValueShortcuts
{
    internal class MyAspect : TypeAspect
    {
        [Introduce]
        public void Method()
        {
            foreach (var field in meta.Target.Type.Fields)
            {
                field.Value = field.Value;
            }
        }
    }

    // <target>
    [MyAspect]
    internal class C
    {
        private int _instanceField = 5;
        private static int _staticField = 6;
    }
}