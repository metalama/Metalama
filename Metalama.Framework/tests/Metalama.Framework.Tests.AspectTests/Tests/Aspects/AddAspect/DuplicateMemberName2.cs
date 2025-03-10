// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.DuplicateMemberName2
{
    // Error: the base class already defines a template and this is not an override.

    internal class Aspect1 : MethodAspect
    {
        [Template]
        public void Template() { }
    }

    internal class Aspect2 : Aspect1
    {
        [Template]
        public new void Template() { }
    }

    // <target>
    internal class Target { }
}