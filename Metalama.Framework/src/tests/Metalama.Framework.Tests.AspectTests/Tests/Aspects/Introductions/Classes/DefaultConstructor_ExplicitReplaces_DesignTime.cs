// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

// https://github.com/metalama/Metalama/issues/1622
// Verifies that when an explicit constructor is introduced on an introduced (nested) class,
// the constructor appears in the design-time generated source.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.DefaultConstructor_ExplicitReplaces_DesignTime
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var introducedType = builder.IntroduceClass( "GeneratedClass" );

            introducedType.IntroduceConstructor(
                nameof(ConstructorTemplate),
                buildConstructor: c =>
                {
                    c.AddParameter( "value", typeof(int) );
                } );
        }

        [Template]
        public void ConstructorTemplate() { }
    }

    // <target>
    [IntroductionAttribute]
    public partial class TargetType { }
}
