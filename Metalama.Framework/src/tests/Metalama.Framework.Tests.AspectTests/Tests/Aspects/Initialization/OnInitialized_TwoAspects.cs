// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_TwoAspects;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(FirstAspect), typeof(SecondAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_TwoAspects;

public class FirstAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(Template), InitializerKind.OnInitialized );
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( "First" );
    }
}

public class SecondAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(Template), InitializerKind.OnInitialized );
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( "Second" );
    }
}

// <target>
[FirstAspect]
[SecondAspect]
public class TargetCode
{
}
