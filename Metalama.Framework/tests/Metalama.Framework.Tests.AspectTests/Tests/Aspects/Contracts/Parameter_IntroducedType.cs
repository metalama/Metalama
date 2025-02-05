using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Parameter_IntroducedType
{
    internal class TestAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var introducedType =
                builder.IntroduceClass(
                        "IntroducedType",
                        buildType: b => { b.Accessibility = Accessibility.Public; } )
                    .Declaration;

            // TODO: It's now necessary to translate the introduced type.

            var introducedMethod =
                builder.With( builder.Target.ForCompilation( builder.Advice.MutableCompilation ) )
                    .IntroduceMethod(
                        nameof(IntroducedMethodTemplate),
                        buildMethod: b => { b.AddParameter( "p", introducedType ); } )
                    .Declaration;

            builder.With( introducedMethod.Parameters.Single() ).AddContract( nameof(ValidateTemplate) );
        }

        [Template]
        public void IntroducedMethodTemplate() { }

        [Template]
        public void ValidateTemplate( dynamic? value )
        {
            if (value == null)
            {
                throw new ArgumentNullException( meta.Target.Parameter.Name );
            }
        }
    }

    // <target>
    [Test]
    internal class Target { }
}