// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Position_TwoAspects_Ordering;
using System;

// Regression test for https://github.com/metalama/Metalama/issues/1582.
// AspectOrder(RunTime, A, B) makes AspectA the outer layer (runtime-first = outermost per the
// matryoshka model). For InitializerPosition.BeforeBase/AfterBase on OnConstructed, the outer
// aspect must bracket the inner one: A.BeforeBase then B.BeforeBase, then B.AfterBase then
// A.AfterBase. The bug inverts both buckets — the inner aspect runs first in BeforeBase and
// the outer aspect runs first in AfterBase.
[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(AspectAAttribute), typeof(AspectBAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Position_TwoAspects_Ordering;

public class AspectAAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(Before), InitializerKind.AfterLastInstanceConstructor, InitializerPosition.BeforeBase );
        builder.AddInitializer( nameof(After), InitializerKind.AfterLastInstanceConstructor, InitializerPosition.AfterBase );
    }

    [Template]
    private void Before() => Console.WriteLine( "AspectA.BeforeBase" );

    [Template]
    private void After() => Console.WriteLine( "AspectA.AfterBase" );
}

public class AspectBAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(Before), InitializerKind.AfterLastInstanceConstructor, InitializerPosition.BeforeBase );
        builder.AddInitializer( nameof(After), InitializerKind.AfterLastInstanceConstructor, InitializerPosition.AfterBase );
    }

    [Template]
    private void Before() => Console.WriteLine( "AspectB.BeforeBase" );

    [Template]
    private void After() => Console.WriteLine( "AspectB.AfterBase" );
}

// <target>
[AspectA]
[AspectB]
public class BaseClass
{
    public BaseClass() { }
}
