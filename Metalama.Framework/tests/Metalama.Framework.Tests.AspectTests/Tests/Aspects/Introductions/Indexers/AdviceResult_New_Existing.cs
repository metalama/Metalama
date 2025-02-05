using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Indexers.AdviceResult_New_Existing
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceIndexer(
                typeof(int),
                nameof(GetTemplate),
                nameof(SetTemplate),
                whenExists: OverrideStrategy.New );

            if (result.Outcome != AdviceOutcome.Error)
            {
                throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Error." );
            }

            if (result.AdviceKind != AdviceKind.IntroduceIndexer)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceIndexer." );
            }

            // TODO: #33060
            //if (!builder.Target.Compilation.Comparers.Default.Equals(
            //        result.Declaration.ForCompilation(builder.Advice.MutableCompilation), 
            //        builder.Target.Indexers.Single()))
            //{
            //    throw new InvalidOperationException($"Declaration was not correct.");
            //}
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

    // <target>
    [TestAspect]
    public class TargetClass
    {
        public int this[ int index ]
        {
            get
            {
                Console.WriteLine( "Original code." );

                return 42;
            }
            set
            {
                Console.WriteLine( "Original code." );
            }
        }
    }
}