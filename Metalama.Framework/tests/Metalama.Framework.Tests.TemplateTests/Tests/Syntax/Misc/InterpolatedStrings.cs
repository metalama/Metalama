using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.AspectTests.Templating.InterpolatedString
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Neutral is compile-time.
            var neutral = $"Zero={0,-5:x}";
            Console.WriteLine( neutral );

            // Compile-time with formatting
            Console.WriteLine( $"ParameterCount={meta.Target.Parameters.Count,-5:x}" );

            // Dynamic.
            var dy = $"Value={meta.Target.Parameters[0].Value,-5:x}";

            // Run-time expression that would cause global::.
            var rt = $"Value={Environment.Version}";

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