#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER) - Index and Range are not included in .Net Framework
#endif

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Metalama.Framework.Tests.AspectTests.Aspects.Misc.IndexAndRangeErrors2
{
    public class UseIndexAndRangeAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var collection = meta.Target.Method.DeclaringType.BaseType!.TypeArguments.Select( ta => ta.ToDisplayString() ).ToArray();
            var runTimeCollection = meta.RunTime( collection ).AsSpan();

            var runTimeCollectionWithCompileTimeIndex = runTimeCollection[meta.CompileTime( ^1 )];
            Console.WriteLine( runTimeCollectionWithCompileTimeIndex );
            var runTimeCollectionWithCompileTimeRange = runTimeCollection[meta.CompileTime( ..^1 )].Length;
            Console.WriteLine( runTimeCollectionWithCompileTimeRange );

            return meta.Proceed();
        }
    }

    internal class GenericType<T1, T2> { }

    internal class TargetCode : GenericType<int, int>
    {
        [UseIndexAndRange]
        private int Method( int a, int b, int c, int d )
        {
            return a;
        }
    }
}