// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
