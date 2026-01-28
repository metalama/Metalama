// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_Contract_OnReceiver_Method;

internal class MyTypeAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var extensionBlock in builder.Target.ExtensionBlocks )
        {
            builder.With( extensionBlock.ReceiverParameter ).AddContract( nameof(ValidateTemplate) );
        }
    }

    [Template]
    private void ValidateTemplate( dynamic? value )
    {
        Console.WriteLine( $"Contract on receiver: {value}, Member: {meta.Target.Member}" );
    }
}

// <target>
[MyTypeAspect]
internal static class C
{
    extension( string test )
    {
        public void SimpleMethod()
        {
            Console.WriteLine( "Simple method." );
        }

        public int MethodWithReturn()
        {
            Console.WriteLine( "Method with return." );
            return 42;
        }

        public void MethodWithParams( int x, string y )
        {
            Console.WriteLine( $"Method with params: {x}, {y}" );
        }

        public async Task AsyncMethod()
        {
            Console.WriteLine( "Async method." );
            await Task.Yield();
        }

        public async Task<int> AsyncMethodWithReturn()
        {
            Console.WriteLine( "Async method with return." );
            await Task.Yield();
            return 42;
        }

        public IEnumerable<int> IteratorMethod()
        {
            Console.WriteLine( "Iterator method." );
            yield return 1;
            yield return 2;
        }
    }
}
#endif
