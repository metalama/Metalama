// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.ParameterMapping
{
    /*
     * Verifies that template parameter is correctly mapped to "value".
     */

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceProperty(
                "Property_NameConflict",
                nameof(GetTemplate),
                nameof(SetTemplate) );
        }

        [Template]
        public int GetTemplate()
        {
            return 42;
        }

        [Template]
        public void SetTemplate( int x )
        {
            var z = x;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}