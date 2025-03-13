// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Combined.ForEachParamIfName
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            foreach (var p in meta.Target.Parameters)
            {
                if (p.Name.Length == 1)
                {
                    Console.WriteLine( "{0} = {1}", p.Name, p.Value );
                }
            }

            foreach (var p in meta.Target.Parameters)
            {
                if (p.Name.StartsWith( "b" ))
                {
                    Console.WriteLine( "{0} = {1}", p.Name, p.Value );
                }
            }

            var result = meta.Proceed();

            return result;
        }
    }

    internal class TargetCode
    {
        private string Method( object a, object bb )
        {
            return a.ToString() + bb.ToString();
        }
    }
}