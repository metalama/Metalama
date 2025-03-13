// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.NeutralObjectInitializers_RunTime
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var runTime1 = new Entity1
            {
                Property1 = meta.Target.Method.Parameters.Count, Property2 = { new Entity2 { Property1 = meta.This.Foo }, new Entity2 { Property1 = 3 } }
            };

            var result = meta.Proceed();

            return result;
        }
    }

    [RunTimeOrCompileTime]
    internal record Entity1
    {
        public int Property1 { get; set; }

        public IList<Entity2> Property2 { get; set; } = new List<Entity2>();
    }

    [RunTimeOrCompileTime]
    internal struct Entity2
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