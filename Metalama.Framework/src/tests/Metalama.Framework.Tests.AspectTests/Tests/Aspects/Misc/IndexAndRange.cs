// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER) - Index and Range are not included in .Net Framework
#endif

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Metalama.Framework.Tests.AspectTests.Aspects.Misc.IndexAndRange
{
    public class UseIndexAndRangeAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var collection = meta.Target.Method.DeclaringType.BaseType!.TypeArguments.Select( ta => ta.ToDisplayString() ).ToArray();
            var compileTimeCollection = collection.AsSpan();
            var runTimeCollection = meta.RunTime( collection ).AsSpan();

            var compileTimeCollectionWithCompileTimeIndex = compileTimeCollection[^1];
            Console.WriteLine( compileTimeCollectionWithCompileTimeIndex );
            var compileTimeCollectionWithCompileTimeRange = compileTimeCollection[..^1].Length;
            Console.WriteLine( compileTimeCollectionWithCompileTimeRange );

            var runTimeCollectionWithRunTimeIndex = runTimeCollection[meta.RunTime( ^1 )];
            Console.WriteLine( runTimeCollectionWithRunTimeIndex );
            var runTimeCollectionWithRunTimeRange = runTimeCollection[meta.RunTime( ..^1 )].Length;
            Console.WriteLine( runTimeCollectionWithRunTimeRange );

            return meta.Proceed();
        }

        [CompileTime]
        private void GetDataClassProperties( INamedType baseType )
        {
            var secondToLastBaseTypeTypeArgument = (INamedType)baseType.TypeArguments[^1];
            var numberOfbaseTypeTypeArgumentsExceptTheLastOne = baseType.TypeArguments.ToArray().AsSpan()[..^1].Length;
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