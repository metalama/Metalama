// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Lambdas.Bug28768
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // The cast to IEnumerable is to avoid using the LinqExtensions class in the engine project.

            var parameterNamesTypes =
                meta.RunTime( ( (IEnumerable<IParameter>)meta.Target.Parameters ).Select( p => ( (IParameter)p ).Type.ToType() ).ToArray() );

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private int Method( int a, string b )
        {
            return a;
        }
    }
}