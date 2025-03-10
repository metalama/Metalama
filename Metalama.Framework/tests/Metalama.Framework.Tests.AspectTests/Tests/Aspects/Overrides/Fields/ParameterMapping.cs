// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.ParameterMapping
{
    /*
     * Verifies that template parameter is correctly mapped by index.
     */

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.Fields.Single() )
                .OverrideAccessors(
                    null,
                    nameof(RenamedValueParameter) );
        }

        [Template]
        public void RenamedValueParameter( int x )
        {
            x = 42;
            meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public int Value;
    }
}