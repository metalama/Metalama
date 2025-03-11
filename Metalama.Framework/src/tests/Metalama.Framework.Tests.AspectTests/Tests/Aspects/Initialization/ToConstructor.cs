// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.ToConstructor;

#pragma warning disable CS0414

public class MyAspect : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.AddInitializer( nameof(Initialize) );
    }

    [Introduce]
    private int _f;

    [Template]
    private void Initialize()
    {
        _f = 5;
    }
}

// CS0414 is restored when the aspect is transformed, so we need to suppress it again.
#pragma warning disable CS0414

// <target>
public class C
{
    [MyAspect]
    public C() { }

    // The initializer should not be added here.
    public C( int c ) { }
}