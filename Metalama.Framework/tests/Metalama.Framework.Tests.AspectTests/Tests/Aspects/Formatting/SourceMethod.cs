// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Formatting.SourceMethod
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var method in builder.Target.Methods)
            {
                builder.Advice.Override( method, nameof(OverrideMethod) );
            }
        }

        [Template]
        private dynamic OverrideMethod()
        {
            if (meta.Target.Method.Invoke( ) > 0)
            {
                var z = meta.Proceed();

                return z - 1;
            }

            return 0;
        }
    }

    // <target>
    [TestAspect]
    public class Target
    {
        public int Foo()
        {
            return 10;
        }
    }
}