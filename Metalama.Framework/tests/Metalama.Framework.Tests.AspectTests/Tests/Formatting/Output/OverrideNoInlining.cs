using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Tests.AspectTests.Tests.Formatting.OverrideNoInlining;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(MyOverrideMethod), typeof(MyOverrideProperty) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.OverrideNoInlining
{
    public class MyOverrideMethod : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "Generated code." );

            try
            {
                meta.Proceed();

                return meta.Proceed();
            }
            catch (Exception)
            {
                Console.WriteLine( "Oops!" );

                throw;
            }
        }
    }

    public class MyOverrideProperty : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get
            {
                _ = meta.Proceed();

                return meta.Proceed();
            }
            set
            {
                meta.Proceed();
                meta.Proceed();
            }
        }
    }

    public class TargetCode
    {
        [MyOverrideMethod]
        public int Method()
        {
            Console.WriteLine( "User code." );

            return 1;
        }

        [MyOverrideProperty]
        public int Property { get; set; }
    }
}