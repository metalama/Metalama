// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable CS0618

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Project;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.ProjectFabricConfigure
{
    internal class Fabric : ProjectFabric
    {
        public override void AmendProject( IProjectAmender amender )
        {
            amender.Project.Extension<Configuration>().Message = "Hello, world.";
        }
    }

    internal class Configuration : ProjectExtension
    {
        public string? Message { get; set; }
    }

    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( meta.Target.Project.Extension<Configuration>().Message );

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        private string Method2( string s ) => s;
    }
}