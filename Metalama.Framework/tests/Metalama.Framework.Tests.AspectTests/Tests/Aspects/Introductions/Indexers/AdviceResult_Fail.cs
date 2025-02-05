using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Indexers.AdviceResult_Fail
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceIndexer(
                typeof(int),
                nameof(GetTemplate),
                nameof(SetTemplate),
                whenExists: OverrideStrategy.Fail );

            if (result.Outcome != AdviceOutcome.Error)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Ignored." );
            }

            if (result.AdviceKind != AdviceKind.IntroduceIndexer)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceIndexer." );
            }

            // TODO: #33060
            //if (result.Declaration != builder.Target.Indexers.Single())
            //{
            //    throw new InvalidOperationException($"Declaration was not correct.");
            //}
        }

        [Template]
        public int GetTemplate( int index )
        {
            return meta.Proceed();
        }

        [Template]
        public void SetTemplate( int index, int value )
        {
            meta.Proceed();
        }
    }

    // <target>
    [TestAspect]
    public class TargetClass
    {
        public int this[ int index ]
        {
            get => 42;
            set { }
        }
    }
}