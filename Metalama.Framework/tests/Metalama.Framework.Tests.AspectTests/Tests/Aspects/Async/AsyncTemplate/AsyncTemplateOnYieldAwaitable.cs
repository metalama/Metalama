using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.AsyncTemplateOnYieldAwaitable
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
            Console.WriteLine( "Normal template." );

            return meta.Proceed();
        }

        [Template]
        public async Task<dynamic?> OverrideAsyncMethod()
        {
            await Task.Yield();
            var result = await meta.ProceedAsync();
            Console.WriteLine( $"result={result}" );

            return result;
        }
    }

    // <target>
    internal class TargetCode
    {
        // The normal template should be applied because YieldAwaitable does not have a method builder.

        [Aspect]
        public YieldAwaitable AsyncMethod( int a )
        {
            return Task.Yield();
        }
    }
}