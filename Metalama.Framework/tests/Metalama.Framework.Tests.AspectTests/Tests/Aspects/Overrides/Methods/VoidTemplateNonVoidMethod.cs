// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.VoidTemplateNonVoidMethod;

public class OverrideAttribute : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Override( nameof(OverrideMethod) );
    }

    [Template]
    public void OverrideMethod( dynamic arg )
    {
        if (arg == null)
        {
            Console.WriteLine( "error" );
            meta.Return( default );
        }

        meta.Return( meta.Proceed() );
    }
}

// <target>
internal class TargetClass
{
    [Override]
    private void VoidMethod( object arg )
    {
        Console.WriteLine( "void method" );
    }

    [Override]
    private int IntMethod( object arg )
    {
        Console.WriteLine( "int method" );

        return 42;
    }
}