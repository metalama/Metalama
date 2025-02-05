using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug31096
{
    public class TestAspect : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            builder.Override( nameof(OverrideMethod) );
        }

        [Template]
        public dynamic? OverrideMethod()
        {
            var result = meta.Proceed();
            var result1 = 42;
            var result2 = 42;
            var result3 = 42;
            Console.WriteLine( "Aspect" + result1 + result2 + result3 );

            return result;
        }
    }

    public class TestData
    {
        public int? Property { get; set; }

        public static bool TryParse1( string str, out int x )
        {
            x = 0;

            return true;
        }

        public static bool TryParse2( string str, out (int x, int y) tuple )
        {
            tuple = ( 0, 0 );

            return true;
        }
    }

    // <target>
    public class TargetClass
    {
        [TestAspect]
        public void TestMethod1()
        {
            var td = new TestData() { Property = TestData.TryParse1( "42", out var result ) ? result : null };
        }

        [TestAspect]
        public void TestMethod2()
        {
            var td = new TestData() { Property = TestData.TryParse2( "42", out var result ) ? result.x : null };
        }

        [TestAspect]
        public void TestMethod3()
        {
            var (result1, (result2, result3)) = ( 42, ( 42, 42 ) );
        }

        [TestAspect]
        public void TestMethod4()
        {
            var (result1, (result2, result3)) = ( 42, ( 42, 42 ) );
        }

        [TestAspect]
        public void TestMethod5()
        {
            var (result1, (result2, result3)) = ( 42, ( 42, 42 ) );
        }
    }
}