// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0618

namespace Metalama.Framework.Tests.AspectTests.Templating.Misc.ElementAccessError
{
    class Aspect
    {
        [TestTemplate]
        dynamic Template()
        {
            var lastParameter = meta.Target.Parameters.Select(p => p.Name).ToArray()[meta.RunTime(0)];

           return 0;
        }
    }

    class TargetCode
    {
        int Method(int a)
        {
            return a;
        }
    }
}