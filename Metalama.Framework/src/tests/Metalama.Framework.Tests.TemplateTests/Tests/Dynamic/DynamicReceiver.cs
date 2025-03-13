// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.DynamicReceiver
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // This
            meta.This.MyMethod();
            meta.This.MyMethod().More();
            meta.This.Value = 5;
            meta.This.MyMethod().More().Value = 5;

            // Parameter
            meta.Target.Parameters[0].Value.MyMethod();
            meta.Target.Parameters[0].Value.MyMethod().More();

            meta.ThisType.Hello();

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

        public static void Hello() { }
    }
}