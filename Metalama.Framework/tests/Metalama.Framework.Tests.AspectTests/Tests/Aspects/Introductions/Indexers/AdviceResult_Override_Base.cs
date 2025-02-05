using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Indexers.AdviceResult_Override_Base
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceIndexer(
                typeof(int),
                nameof(GetTemplate),
                nameof(SetTemplate),
                whenExists: OverrideStrategy.Override );

            if (result.Outcome != AdviceOutcome.Override)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Override." );
            }

            if (result.AdviceKind != AdviceKind.IntroduceIndexer)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceIndexer." );
            }

            if (!builder.Advice.MutableCompilation.Comparers.Default.Equals(
                    result.Declaration.ForCompilation( builder.Advice.MutableCompilation ),
                    builder.Target.ForCompilation( builder.Advice.MutableCompilation ).Indexers.Single() ))
            {
                throw new InvalidOperationException( $"Declaration was not correct." );
            }
        }

        [Template]
        public int GetTemplate( int index )
        {
            Console.WriteLine( "Aspect code." );

            return meta.Proceed();
        }

        [Template]
        public void SetTemplate( int index, int value )
        {
            Console.WriteLine( "Aspect code." );
            meta.Proceed();
        }
    }

    public class BaseClass
    {
        public virtual int this[ int index ]
        {
            get => 42;
            set { }
        }
    }

    // <target>
    [TestAspect]
    public class TargetClass : BaseClass { }
}