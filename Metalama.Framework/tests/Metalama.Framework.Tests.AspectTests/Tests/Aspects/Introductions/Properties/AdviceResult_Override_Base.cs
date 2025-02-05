using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Properties.AdviceResult_Override_Base
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceProperty( nameof(Property), whenExists: OverrideStrategy.Override );

            if (result.Outcome != AdviceOutcome.Override)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Override." );
            }

            if (result.AdviceKind != AdviceKind.IntroduceProperty)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceProperty." );
            }

            if (!builder.Advice.MutableCompilation.Comparers.Default.Equals(
                    result.Declaration.ForCompilation( builder.Advice.MutableCompilation ),
                    builder.Target.ForCompilation( builder.Advice.MutableCompilation ).Properties.Single() ))
            {
                throw new InvalidOperationException( $"Declaration was not correct." );
            }
        }

        [Template]
        public int Property
        {
            get
            {
                Console.WriteLine( "Aspect code." );

                return meta.Proceed();
            }
            set
            {
                Console.WriteLine( "Aspect code." );
                meta.Proceed();
            }
        }
    }

    public class BaseClass
    {
        public virtual int Property
        {
            get => 42;
            set { }
        }
    }

    // <target>
    [TestAspect]
    public class TargetClass : BaseClass { }
}