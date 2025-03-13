// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.Syntax.Misc.ConditionalExpressionError
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var extraField = meta.Target.Method.DeclaringType.Fields.OfName( "extra" ).SingleOrDefault();

            // compile-time condition, mix of run-time and compile-time results
            Console.WriteLine( extraField != null ? extraField.Name : meta.RunTime( "undefined" ) );

            return null;
        }
    }

    internal class TargetCode
    {
        private void Method() { }
    }
}