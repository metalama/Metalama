using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Order.IntroductionAndOverride;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(FirstAttribute), typeof(SecondAttribute), typeof(ThirdAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Order.IntroductionAndOverride
{
    public class FirstAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var method in builder.Target.Methods)
            {
                builder.With( method ).Override( nameof(OverrideTemplate) );
            }
        }

        [Introduce]
        public void IntroducedMethod1()
        {
            Console.Write( "This is introduced by the first aspect." );
        }

        [Template]
        public dynamic? OverrideTemplate()
        {
            try
            {
                Console.Write( "This is overridden by the first aspect." );

                return meta.Proceed();
            }
            finally
            {
                Console.Write( "This is overridden by the first aspect." );
            }
        }
    }

    public class SecondAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var method in builder.Target.Methods)
            {
                builder.With( method ).Override( nameof(OverrideTemplate) );
            }
        }

        [Introduce]
        public void IntroducedMethod2()
        {
            Console.Write( "This is introduced by the second aspect." );
        }

        [Template]
        public dynamic? OverrideTemplate()
        {
            try
            {
                Console.Write( "This is overridden by the second aspect." );

                return meta.Proceed();
            }
            finally
            {
                Console.Write( "This is overridden by the second aspect." );
            }
        }
    }

    public class ThirdAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var method in builder.Target.Methods)
            {
                builder.With( method ).Override( nameof(OverrideTemplate) );
            }
        }

        [Introduce]
        public void IntroducedMethod3()
        {
            Console.Write( "This is introduced by the third aspect." );
        }

        [Template]
        public dynamic? OverrideTemplate()
        {
            try
            {
                Console.Write( "This is overridden by the third aspect." );

                return meta.Proceed();
            }
            finally
            {
                Console.Write( "This is overridden by the third aspect." );
            }
        }
    }

    // <target>
    [First]
    [Second]
    [Third]
    internal class TargetClass
    {
        public int Method()
        {
            return 42;
        }
    }
}