// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.TemplateTests.Return.ReturnInLambda
{
    [CompileTime]
    internal class Aspect
    {
        // A return inside a lambda should NOT stop the outer template.
        // The lambda has its own scope; its return is independent.
        [TestTemplate]
        private dynamic? Template()
        {
            // Define a run-time lambda with a return - use meta.RunTime to clarify scope
            var lambda = meta.RunTime( ( object? input ) =>
            {
                if ( input == null )
                {
                    return "default";  // This return is in the lambda, not the template
                }

                return input;
            } );

            // Call the lambda (its return value is used here)
            var result = lambda( meta.Proceed() );

            // This code SHOULD be reached - the lambda's return doesn't stop the template
            return result;
        }
    }

    internal class TargetCode
    {
        // <target>
        private object? Method()
        {
            return null;
        }
    }
}
