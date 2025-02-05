using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.CompileTimeOutVarArg
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var d = meta.CompileTime( new Dictionary<int, int>() );
            d.Add( 0, 5 );
            d.TryGetValue( 0, out var x );
            meta.InsertComment( "x = " + x );

            return null;
        }
    }

    internal class TargetCode
    {
        private int Method( int a )
        {
            return a;
        }
    }
}