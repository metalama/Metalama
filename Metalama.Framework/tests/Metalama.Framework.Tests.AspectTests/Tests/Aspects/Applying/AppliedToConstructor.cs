using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Applying.AppliedToConstructor
{
    public class MyAspect : ConstructorAspect
    {
        public override void BuildAspect( IAspectBuilder<IConstructor> builder )
        {
            throw new Exception( "Oops" );
        }
    }

    // <target>
    internal class TargetClass
    {
        [MyAspect]
        private TargetClass() { }
    }
}