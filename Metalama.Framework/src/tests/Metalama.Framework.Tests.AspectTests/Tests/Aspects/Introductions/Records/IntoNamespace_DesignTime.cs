// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

// Verifies that a top-level introduced record reaches the design-time generated source with its positional
// parameter list and its modifiers. A top-level introduced type has no declaration outside the generated file, so
// the file carries the declaration that the introduction transformation produces rather than a partial part
// re-created from the type kind, which would drop the parameter list and the readonly modifier.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.IntoNamespace_DesignTime
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var target = builder.With( builder.Target.ContainingNamespace );

            target.IntroduceRecord(
                "TopLevelPositional",
                buildRecord: r =>
                {
                    r.Accessibility = Accessibility.Public;
                    r.AddPositionalParameter( "Name", typeof(string) );
                    r.AddPositionalParameter( "Count", typeof(int) );
                } );

            target.IntroduceRecord(
                "TopLevelReadOnlyStruct",
                RecordKind.Struct,
                buildRecord: r =>
                {
                    r.Accessibility = Accessibility.Public;
                    r.IsReadOnly = true;
                    r.AddPositionalParameter( "X", typeof(double) );
                    r.AddPositionalParameter( "Y", typeof(double) );
                } );
        }
    }

    // <target>
    namespace TargetNamespace
    {
        [Introduction]
        public class TargetType { }
    }
}
