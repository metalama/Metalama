// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Dependency;

internal class GenericTemplateAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Advice.Override( builder.Target, nameof(Template), new { T = typeof(int) } );
    }

    [Template]
    private static void Template<[CompileTime] T>( T arg )
    {
        Console.WriteLine( arg.GetType() );

        meta.Proceed();
    }
}
