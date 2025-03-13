// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Tests.AspectTests.TestInputs.Templating.NamespaceExpansion.PartiallyQualified.ChildNs;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Templating.NamespaceExpansion
{
    namespace PartiallyQualified
    {
        [CompileTime]
        internal class Aspect
        {
            [TestTemplate]
            private dynamic? Template()
            {
                var c = new ChildClass();

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

        namespace ChildNs
        {
            internal class ChildClass { }
        }
    }
}