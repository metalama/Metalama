using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.TestFramework.Html.Introduction
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public void IntroducedMethod()
        {
            Console.WriteLine( "This is introduced method." );
            meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}