using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Applying.AppliedToGenericParameter
{
    public class MyAspect : TypeParameterAspect
    {
        public override void BuildAspect( IAspectBuilder<ITypeParameter> builder )
        {
            throw new Exception( "Oops" );
        }
    }

    // <target>
    internal class TargetClass
    {
        private void M<[MyAspect] T>( int a ) { }
    }
}