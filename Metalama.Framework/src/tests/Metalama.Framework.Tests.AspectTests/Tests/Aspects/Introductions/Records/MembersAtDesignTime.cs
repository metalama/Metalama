// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

// Verifies that members introduced into a nested introduced record and into a nested introduced struct reach the
// design-time generated source. Such a type is the key of a bucket of its own, and that bucket is processed together
// with the transformation that introduces the type, so one generated document per introduced type carries the
// declaration and the members. The record shows that the declaration keeps its accessibility and its positional
// parameter list in that document.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.MembersAtDesignTime
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var record = builder.IntroduceRecord(
                "Positional",
                buildRecord: r =>
                {
                    r.Accessibility = Accessibility.Public;
                    r.AddPositionalParameter( "Value", typeof(int) );
                } );

            builder.With( record.Declaration ).IntroduceMethod( nameof(MethodTemplate) );

            var introducedStruct = builder.IntroduceStruct( "Struct", buildType: t => t.Accessibility = Accessibility.Public );

            builder.With( introducedStruct.Declaration ).IntroduceMethod( nameof(MethodTemplate) );
        }

        [Template]
        public int MethodTemplate() => 42;
    }

    // <target>
    [Introduction]
    public partial class TargetType { }
}
