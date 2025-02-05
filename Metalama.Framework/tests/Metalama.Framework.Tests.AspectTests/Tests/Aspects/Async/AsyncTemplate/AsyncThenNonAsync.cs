using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.AsyncThenNonAsync;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect1), typeof(Aspect2) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.AsyncThenNonAsync
{
    internal class Aspect1 : MethodAspect
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
        public dynamic OverrideMethod()
        {
            throw new NotSupportedException( "This should not be called." );
        }

        [Template]
        public async Task<dynamic?> OverrideAsyncMethod()
        {
            Console.WriteLine( "Async intercept" );
            await Task.Yield();
            var result = await meta.ProceedAsync();

            return result;
        }
    }

    internal class Aspect2 : MethodAspect
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
        public dynamic OverrideMethod()
        {
            throw new NotSupportedException( "This should not be called." );
        }

        [Template]
        public Task<dynamic?> OverrideAsyncMethod()
        {
            Console.WriteLine( "Non-async intercept" );

            return meta.ProceedAsync();
        }
    }

    // <target>
    internal class TargetCode
    {
        // The normal template should be applied because YieldAwaitable does not have a method builder.

        [Aspect1]
        [Aspect2]
        public async Task<int> AsyncMethod( int a )
        {
            await Task.Yield();

            return a;
        }

        [Aspect1]
        [Aspect2]
        public Task<int> NonAsyncMethod( int a )
        {
            return Task.FromResult( a );
        }
    }
}