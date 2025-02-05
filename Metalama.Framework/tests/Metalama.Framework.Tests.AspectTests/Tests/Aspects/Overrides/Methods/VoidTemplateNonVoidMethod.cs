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