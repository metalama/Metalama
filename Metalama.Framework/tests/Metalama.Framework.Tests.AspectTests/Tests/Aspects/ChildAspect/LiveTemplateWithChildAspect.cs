// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(LiveTemplate)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Code;
using Metalama.Testing.AspectTesting;

namespace Metalama.Framework.IntegrationTests.LiveTemplates.LiveTemplateWithChildAspect;

class CommonAspect : TypeAspect
{
    [Introduce]
    int i;
}

[EditorExperience(SuggestAsLiveTemplate = true)]
[Inheritable]
internal class LiveAspect : TypeAspect
{
    [Introduce]
    int j;

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.Outbound.AddAspect<CommonAspect>();
    }
}

// <target>
[TestLiveTemplate(typeof(LiveAspect))]
class Target
{
}