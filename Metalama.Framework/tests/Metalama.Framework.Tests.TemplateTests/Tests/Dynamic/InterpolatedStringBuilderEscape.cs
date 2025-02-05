using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.InterpolatedStringBuilderEscape
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Normal literals.
            Console.WriteLine( "\\\n{}\"" );
            Console.WriteLine( meta.CompileTime( "\\" + "\n{}\"" ) );

            // Interpolated string.
            var s = new InterpolatedStringBuilder();
            s.AddText( "{ " );
            s.AddText( "$" );
            s.AddText( "\\" );
            s.AddText( "\n" );
            s.AddText( " }" );

            var a = s.ToValue();

            return default;
        }
    }

    // <target>
    internal class TargetCode
    {
        private int Method( int a, string c, DateTime dt )
        {
            return a;
        }
    }
}