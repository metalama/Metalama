// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.With
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var runTime1 = meta.RunTime( new Entity1 { Property1 = meta.Target.Method.Parameters.Count } );

            var runTime2 = runTime1 with { Property1 = meta.This.Foo };

            var result = meta.Proceed();

            return result;
        }
    }

    [RunTimeOrCompileTime]
    internal record Entity1
    {
        public int Property1 { get; set; }
    }

    internal class TargetCode
    {
        private object Method( object a )
        {
            return a;
        }
    }
}