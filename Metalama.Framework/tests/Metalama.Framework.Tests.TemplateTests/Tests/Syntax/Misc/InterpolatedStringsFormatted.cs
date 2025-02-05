#if TEST_OPTIONS
// @FormatOutput
#endif

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.AspectTests.Templating.InterpolatedStringFormatted
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Neutral.
            var neutral = $"Zero={0,-5:x}";

            // Compile-time with formatting
            Console.WriteLine( $"ParameterCount={meta.Target.Parameters.Count,-5:x}" );

            // Run-time
            var rt = $"Value={meta.Target.Parameters[0].Value,-5:x}";

            // Both
            var both = $"{meta.Target.Type.Fields.Single().Name}={meta.Target.Parameters[0].Value}";

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private int field;

        private int Method( int a )
        {
            return a;
        }
    }
}