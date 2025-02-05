using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.RunTimeMethod
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var list = meta.CompileTime( new List<string>() );
            list.Add( "a" );
            list.Add( "b" );
            list.Add( "c" );
            list.Add( "d" );
            var x = meta.RunTime( list );

            return default;
        }
    }

    // <target>
    internal class TargetCode
    {
        private int Method( int a )
        {
            return a;
        }
    }
}