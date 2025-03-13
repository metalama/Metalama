// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable CS8600, CS8603
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.ReturnStatements.ReturnNull
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            var a = meta.Target.Parameters[0];
            var b = meta.Target.Parameters[1];

            if (a.Value == null || b.Value == null)
            {
                return null;
            }

            var result = meta.Proceed();

            return result;
        }
    }

    internal class TargetCode
    {
        // <target>
        private string Method( string a, string b )
        {
            return a + b;
        }
    }
}