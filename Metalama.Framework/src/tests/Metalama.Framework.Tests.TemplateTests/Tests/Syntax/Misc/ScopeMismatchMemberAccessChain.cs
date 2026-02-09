// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0219 // Variable is assigned but its value is never used

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Misc.ScopeMismatchMemberAccessChain
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // This should report a single LAMA0104 error on 'AppDomain' (the innermost run-time-only node),
            // not three separate errors on 'AppDomain', 'CurrentDomain', and 'GetAssemblies'.
            var assemblies = meta.CompileTime( AppDomain.CurrentDomain.GetAssemblies() );

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private int Method( int a )
        {
            return a;
        }
    }
}
