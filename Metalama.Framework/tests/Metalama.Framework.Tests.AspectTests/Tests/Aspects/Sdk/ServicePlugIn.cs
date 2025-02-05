using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine;
using Metalama.Framework.Project;
using Metalama.Framework.Services;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.ServicePlugIn
{
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var myService = meta.Target.Project.ServiceProvider.GetRequiredService<IMyService>();
            Console.WriteLine( myService.Message );

            return meta.Proceed();
        }
    }

    internal interface IMyService : IProjectService
    {
        string Message { get; }
    }

    [MetalamaPlugIn]
    internal class MyService : IMyService
    {
        public string Message => "Hello, world.";
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        private int Method( int a )
        {
            return a;
        }
    }
}