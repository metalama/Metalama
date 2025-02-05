using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Applying.AppliedToParameter
{
    public class MyAspect : ParameterAspect
    {
        public override void BuildAspect( IAspectBuilder<IParameter> builder )
        {
            throw new Exception( "Oops" );
        }
    }

    // <target>
    internal class TargetClass
    {
        private void M( [MyAspect] int a ) { }
    }
}