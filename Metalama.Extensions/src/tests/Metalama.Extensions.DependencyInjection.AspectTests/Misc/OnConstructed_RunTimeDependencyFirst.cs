// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Extensions.DependencyInjection;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Extensions.DependencyInjection.AspectTests.Misc.OnConstructed_RunTimeDependencyFirst;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(DependencyAspect), typeof(InitializerAspect) )]

namespace Metalama.Extensions.DependencyInjection.AspectTests.Misc.OnConstructed_RunTimeDependencyFirst;

public class DependencyAspect : TypeAspect
{
    [IntroduceDependency]
    private readonly IFormatProvider _formatProvider;
}

public class InitializerAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterLastInstanceConstructor );
    }

    [Template]
    private void InitializerTemplate()
    {
        Console.WriteLine( "OnConstructed" );
    }
}

// <target>
[DependencyAspect]
[InitializerAspect]
public class ImplicitConstructor
{
}

// <target>
[DependencyAspect]
[InitializerAspect]
public class ExplicitConstructors
{
    public ExplicitConstructors() { }

    public ExplicitConstructors( int x ) { }
}
