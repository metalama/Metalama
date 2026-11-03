// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Forwarder_DesignTime;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterLastInstanceConstructor );
    }

    [Template]
    private void InitializerTemplate()
    {
        Console.WriteLine( $"OnConstructed {meta.Target.Type.Name}" );
    }
}

// <target>
[TheAspect]
public partial class ImplicitConstructor
{
}

// <target>
[TheAspect]
public partial class ExplicitConstructors
{
    public ExplicitConstructors()
    {
    }

    public ExplicitConstructors( int value )
    {
        _ = value;
    }
}

internal class Usage
{
    private static T Create<T>()
        where T : new()
        => new T();

    public void M()
    {
        _ = Create<ImplicitConstructor>();
        _ = Create<ExplicitConstructors>();
        _ = new ExplicitConstructors( 1 );
    }
}
