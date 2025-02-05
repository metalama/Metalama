using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Misc.TypeOfBug
{
    [AttributeUsage( AttributeTargets.Field | AttributeTargets.Property )]
    public class NotToStringAttribute : Attribute { }

    public class ToStringAttribute : TypeAspect
    {
        [Introduce]
        public string IntroducedToString()
        {
            var t = meta.CompileTime( typeof(NotToStringAttribute) );
            var n = meta.CompileTime( nameof(NotToStringAttribute) );
            Console.WriteLine( t );

            return n;
        }
    }

    // <target>
    [ToString]
    internal class TargetCode { }
}