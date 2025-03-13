// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.InvalidCode.InvalidLiteralInInvocation
{
    public class EnrichExceptionAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var methodSignatureBuilder = new InterpolatedStringBuilder();

#if TESTRUNNER
            // The next line has an intentional syntax error.
            methodSignatureBuilder.AddText(""(');
#endif

            return meta.Proceed();
        }
    }
}