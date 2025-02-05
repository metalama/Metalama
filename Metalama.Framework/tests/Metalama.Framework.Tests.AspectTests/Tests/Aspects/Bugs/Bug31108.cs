#if TEST_OPTIONS
// @Skipped(#31108)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug31108
{
    public class TestAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            try
            {
                var result = meta.Proceed();

                Console.WriteLine( $"Executing {meta.Target.Method.ToDisplayString()}" );
                var parameters = meta.Target.Parameters;

                if (parameters.Count > 0)
                {
                    foreach (var parameter in parameters)
                    {
                        Console.WriteLine( $"Method has parameter {parameter.Name} of type {parameter.Type} with {parameter.DefaultValue} default value." );
                    }

                    return result;
                }
                else
                {
                    Console.WriteLine( "Parameterless method." );

                    return result;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine( $"Caught exception {e.Message} in {meta.Target.Method.Name}" );

                throw;
            }
        }
    }

    public static class DummyExtensions
    {
        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>( this IEnumerable<T> enumerable ) => throw new NotImplementedException();

        public static IAsyncEnumerable<TResult> SelectAwait<TSource, TResult>(
            this IAsyncEnumerable<TSource> enumerable,
            Func<TSource, ValueTask<TResult>> predicate )
            => throw new NotImplementedException();
    }

    // <target>
    public static class TargetClass
    {
        [TestAspect]
        public static IAsyncEnumerable<TResult> SelectAwait<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, ValueTask<TResult>> predicate )
        {
            return source.ToAsyncEnumerable().SelectAwait( predicate );
        }
    }
}