// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.TemplateProvider_CrossAssembly;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "regular template" );
        var templates = new Templates();
        templates.Template( 1 );
        meta.InvokeTemplate( nameof(Templates.Template), templates, new { i = 2 } );

        return default;
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void Method() { }
}