// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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