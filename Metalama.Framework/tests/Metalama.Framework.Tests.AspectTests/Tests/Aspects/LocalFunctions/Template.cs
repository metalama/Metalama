// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LocalFunctions.Template;

class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Generate();

        Introduce();

        return default;

        [Template]
        void Generate()
        {
        }

        [Introduce]
        void Introduce()
        {
        }
    }
}

class TargetCode
{
    // <target>
    [Aspect]
    void Method()
    {
    }
}