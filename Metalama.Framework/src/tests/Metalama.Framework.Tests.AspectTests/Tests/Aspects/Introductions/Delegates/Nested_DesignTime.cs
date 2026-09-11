// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

// Verifies that a nested introduced delegate reaches the design-time generated source. A nested introduced type is
// emitted as a member of the partial wrapper of its containing type, and the generator adds the partial modifier to
// a class, a struct, an interface and a record only, so the delegate correctly receives none.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Delegates.Nested_DesignTime
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceDelegate(
                "GeneratedHandler",
                buildDelegate: d =>
                {
                    d.Accessibility = Accessibility.Public;
                    d.ReturnType = TypeFactory.GetType( SpecialType.Void );
                    d.AddParameter( "value", typeof(string) );
                } );
        }
    }

    // <target>
    [Introduction]
    public partial class TargetType { }
}
