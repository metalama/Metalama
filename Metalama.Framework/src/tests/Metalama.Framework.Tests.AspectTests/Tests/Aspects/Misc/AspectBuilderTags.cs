// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.AspectBuilderTags;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.Tags = new { TheTag = 1 };
        base.BuildAspect( builder );
        builder.Tags = new { TheTag = 2 };
        builder.IntroduceMethod( nameof(Introduced2) );
        builder.IntroduceMethod( nameof(Introduced3), tags: new { TheTag = 3 } );
        builder.Tags = new { TheTag = 4 };

    }

    [Introduce]
    public void Introduced1()
    {
        // Expected TheTag=4.
        Console.WriteLine($"TheTag={meta.Tags["TheTag"]}");
    }

    [Template]
    public void Introduced2()
    {
        // Expected TheTag=4.
        Console.WriteLine($"TheTag={meta.Tags["TheTag"]}");
    }
    
    [Template]
    public void Introduced3()
    {
        // Expected TheTag=3.
        Console.WriteLine($"TheTag={meta.Tags["TheTag"]}");
    }
}

// <target>
[TheAspect]
public class C { }