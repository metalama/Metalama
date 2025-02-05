using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Parameter_Introduced
{
    // Tests that a contract on introduced ctor parameter work properly.

    internal class TestAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var constructor in builder.Target.Constructors)
            {
                var parameter = builder.With( constructor )
                    .IntroduceParameter(
                        "dependency",
                        typeof(int),
                        TypedConstant.Create( 0 ) )
                    .Declaration;

                builder.With( parameter ).AddContract( nameof(Validate) );
            }
        }

        [Template]
        public void Validate( int value )
        {
            if (value == 0)
            {
                throw new ArgumentOutOfRangeException( meta.Target.Parameter.Name );
            }
        }
    }

    // <target>
    [Test]
    internal class Target
    {
        public Target() { }

        public Target( int x ) { }
    }
}