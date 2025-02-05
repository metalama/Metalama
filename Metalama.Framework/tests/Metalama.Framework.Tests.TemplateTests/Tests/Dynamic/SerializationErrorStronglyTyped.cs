using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.SerializationErrorStronglyTyped
{
    [CompileTime]
    internal class Aspect
    {
        [Introduce]
        void Method(object arg) { }

        [Introduce]
        event Action<object> Event = delegate { };

        [Introduce]
        Action<object> Property { get; } = delegate { };

        [TestTemplate]
        private dynamic? Template()
        {
            Method(meta.RunTime(meta.Target.Method));

            Event(meta.RunTime(meta.Target.Method));

            Property(meta.RunTime(meta.Target.Method));

            return default;
        }
    }

    // <target>
    internal class TargetCode
    {
        private int Method( int a )
        {
            return a;
        }
    }
}