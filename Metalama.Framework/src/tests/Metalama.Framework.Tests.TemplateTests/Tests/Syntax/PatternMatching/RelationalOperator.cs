// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

// TODO: Change the namespace
namespace Metalama.Framework.Tests.AspectTests.PatternMatching.RelationalOperator
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Compile time
            var a1 = meta.CompileTime( meta.Target.Parameters.Count is >= 0 and < 5 );
            meta.InsertComment( "a1 = " + a1 );

            // Run-time
            var a2 = meta.Target.Parameters[0].Value is >= 0 and < 5;

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private int Method( int a )
        {
            return a;
        }
    }
}