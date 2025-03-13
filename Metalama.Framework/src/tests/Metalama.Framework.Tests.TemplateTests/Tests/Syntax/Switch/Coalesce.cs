// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.InternalPipeline.Templating.Syntax.Switch.Coalesce
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Both (evaluted at compile time into a run-time value)
            _ = default(int?) ?? 1;

            // Compile-time
            _ = meta.CompileTime( default(int?) ) ?? 2;
            _ = default(int?) ?? meta.CompileTime( 3 );

            // Run-time
            _ = meta.RunTime( default(int?) ) ?? 4;
            _ = default(int?) ?? meta.RunTime( 5 );

            return default;
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