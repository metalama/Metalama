using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.NonAsyncTemplateOnAsyncMethod
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
        public async ValueTask<int> AsyncMethod( int a )
        {
            await Task.Yield();

            return a;
        }
    }
}