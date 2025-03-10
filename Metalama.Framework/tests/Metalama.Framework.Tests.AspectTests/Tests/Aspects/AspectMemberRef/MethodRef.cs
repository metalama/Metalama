// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.AspectMemberRef.MethodRef
{
    public class RetryAttribute : OverrideMethodAspect
    {
        [CompileTime]
        public string GetParameterName() => ( (IEnumerable<IParameter>)meta.Target.Parameters ).First().Name;

        [CompileTime]
        public static string GetParameterNameStatic( IParameter p ) => p.Name;

        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( GetParameterName() );
            Console.WriteLine( GetParameterNameStatic( ( (IEnumerable<IParameter>)meta.Target.Parameters ).First() ) );

            return meta.Proceed();
        }
    }

    internal class Program
    {
        // <target>
        [Retry]
        private static int Foo( int a )
        {
            return 0;
        }
    }
}