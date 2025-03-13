// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @AcceptInvalidInput
#endif

using System;
using System.Text;
using Metalama.Testing.AspectTesting;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.New.NewInvalidType
{
    class Aspect
    {
        [TestTemplate]
        dynamic? Template()
        {
            var o = new NonExistingType();
            
            return meta.Proceed();
        }
    }

    // <target>
    class TargetCode
    {
        int Method(int a)
        {
            return a;
        }
    }
}