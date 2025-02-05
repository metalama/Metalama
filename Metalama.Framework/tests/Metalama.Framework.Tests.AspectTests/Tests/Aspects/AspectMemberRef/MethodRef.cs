using System;
using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.AspectMemberRef.MethodRef
{
    public class RetryAttribute : OverrideMethodAspect
    {
        [CompileTime]
        public string GetParameterName() => ( (IEnumerable<IParameter>)meta.Target.Parameters ).First().Name;

        [CompileTime]
        public static string GetParameterNameStatic( IParameter p ) => p.Name;

        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( GetParameterName() );
            Console.WriteLine( GetParameterNameStatic( ( (IEnumerable<IParameter>)meta.Target.Parameters ).First() ) );

            return meta.Proceed();
        }
    }

    internal class Program
    {
        // <target>
        [Retry]
        private static int Foo( int a )
        {
            return 0;
        }
    }
}