#pragma warning disable CS8600, CS8603
using System;
using Metalama.Framework.Code;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.TypeOf.TypeOfCompileTimeType
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            var rt = meta.RunTime( typeof(string) );
            var ct = typeof(string);
            Console.WriteLine( "rt=" + rt );
            Console.WriteLine( "ct=" + ct );

            if (( (IParameter)meta.Target.Parameters[0] ).Type.IsConvertibleTo( typeof(string) )) { }

            return meta.Proceed();
        }
    }

    [CompileTime]
    public class MyClass1 { }

    internal class TargetCode
    {
        private string Method( string a )
        {
            return a;
        }
    }
}