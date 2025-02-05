using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LocalFunctions.Parameter_Simple;

internal class Aspect : TypeAspect
{
    [Template]
    private void M()
    {
        Log( "foo" );

        void Log( string instance ) => Console.WriteLine( instance );
    }

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(M) );
    }
}

// <target>
[Aspect]
internal class C { }