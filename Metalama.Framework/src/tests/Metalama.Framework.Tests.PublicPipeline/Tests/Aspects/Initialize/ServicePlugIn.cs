// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Sdk;
using Metalama.Framework.Project;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Initialize.ServicePlugIn
{
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var myService = meta.Target.Project.ServiceProvider.GetService<IMyService>();
            Console.WriteLine( myService.Message );

            return meta.Proceed();
        }
    }

    internal interface IMyService : IService
    {
        string Message { get; }
    }

    [CompilerPlugin]
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