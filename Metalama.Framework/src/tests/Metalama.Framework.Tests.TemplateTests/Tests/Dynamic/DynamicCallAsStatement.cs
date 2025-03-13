// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @AllowCompileTimeDynamicCode
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.DynamicCallAsStatement
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Expression statement
            meta.Target.Method.Invoke( 1 + 1 ).Foo();
            meta.Target.Method.Invoke( meta.Target.Parameters[0].Value ).Foo();
            meta.Target.Method.Invoke( 1 + 1 );
            meta.Target.Method.Invoke( meta.Target.Parameters[0].Value );

            // Discard assignment
            _ = meta.Target.Method.Invoke( 1 + 1 ).Foo();
            _ = meta.Target.Method.Invoke( meta.Target.Parameters[0].Value ).Foo();
            _ = meta.Target.Method.Invoke( 1 + 1 );
            _ = meta.Target.Method.Invoke( meta.Target.Parameters[0].Value );

            // Local variable assignment
            var x = meta.Target.Method.Invoke( 1 + 1 ).Foo();
            var y = meta.Target.Method.Invoke( meta.Target.Parameters[0].Value ).Foo();
            var a = meta.Target.Method.Invoke( 1 + 1 );
            var b = meta.Target.Method.Invoke( meta.Target.Parameters[0].Value );

            return default;
        }
    }

    // <target>
    internal class TargetCode
    {
        private int Method( int a )
        {
            return a;
        }
    }
}