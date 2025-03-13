// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.Syntax.Tuples.ReturnCompileTimeTuple
{
    internal class Aspect
    {
        [TestTemplate]
        private (int, Type) Template()
        {
            var t = ( meta.Target.Method.Parameters.Count, typeof(int) );

            return ( t.Count + 1, t.Item2 );
        }
    }

    internal class TargetCode
    {
        private (int, Type) Method( int a )
        {
            return meta.CompileTime( ( a, typeof(int) ) );
        }
    }
}