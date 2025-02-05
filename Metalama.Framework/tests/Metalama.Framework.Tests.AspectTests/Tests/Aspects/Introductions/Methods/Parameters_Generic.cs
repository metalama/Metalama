using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.Parameters_Generic
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceMethod(
                nameof(Template),
                buildMethod: introduced =>
                {
                    introduced.Name = "IntroducedMethod";
                    introduced.AddParameter( "x", builder.Target );
                } );

            // TODO: Other members.
        }

        [Template]
        public dynamic? Template()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass<T> { }
}