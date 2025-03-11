// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.SkipAspect;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(IsSkippedAspect), typeof(SkippedAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.SkipAspect;

public class SkippedAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.SkipAspect();
    }

    [Introduce]
    public static bool IsApplied = true;
}

public class IsSkippedAspect : TypeAspect
{
    [Introduce]
    public static bool IsSkipped
        = meta.Target.Type.Enhancements().GetAspectInstances().Where( a => a.Aspect is SkippedAspect ).Single().IsSkipped;
}

// <target>
[SkippedAspect]
[IsSkippedAspect]
public class C { }