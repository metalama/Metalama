// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.NonAsyncTemplateOnNonAsyncMethod
{
    internal class Aspect : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            builder.Override(
                new MethodTemplateSelector(
                    nameof(OverrideMethod),
                    asyncTemplate: nameof(OverrideAsyncMethod),
                    useAsyncTemplateForAnyAwaitable: true ) );
        }

        [Template]
        public dynamic? OverrideMethod()
        {
            Console.WriteLine( "Should not be selected." );

            return meta.Proceed();
        }

        [Template]
        public Task<dynamic?> OverrideAsyncMethod()
        {
            Console.WriteLine( "Getting task" );
            var task = meta.ProceedAsync()!;
            Console.WriteLine( "Got task" );

            return task;
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        public ValueTask<int> AsyncMethod( int a )
        {
            return new ValueTask<int>( Task.FromResult( a ) );
        }
    }
}