using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LocalFunctions.Parameter_CompileTime;

internal class Aspect : TypeAspect
{
    [Template]
    private void M()
    {
        LogMethod( null );
        LogString( "foo" );

        void LogMethod( IMethod? instance ) => Console.WriteLine( instance?.ToString() );

        void LogString( [CompileTime] string instance ) => Console.WriteLine( instance );
    }

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(M) );
    }
}

// <target>
[Aspect]
internal class C { }