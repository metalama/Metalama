// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.CustomTemplateProvider;

internal class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.Advice.WithTemplateProvider( new MyTemplateProvider() )
            .IntroduceMethod( builder.Target, nameof(MyTemplateProvider.NewMethod) );
    }
}

internal class MyTemplateProvider : ITemplateProvider
{
    [Template]
    public void NewMethod()
    {
        Console.WriteLine( "Hello, world." );
    }
}

// <target>
[MyAspect]
internal class Target { }