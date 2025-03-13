// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable CS8600, CS8603
using System;
using Metalama.Framework.Code;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.TypeOf.NameOfRunTimeType
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            var rt = meta.RunTime( nameof(MyClass1) );
            var ct = nameof(MyClass1);
            Console.WriteLine( "rt=" + rt );
            Console.WriteLine( "ct=" + ct );

            if (( (IParameter)meta.Target.Parameters[0] ).Type is INamedType { Name: nameof(MyClass1) })
            {
                Console.WriteLine( "Oops" );
            }

            Console.WriteLine( nameof(MyClass1) );

            Console.WriteLine( nameof(MyClass1.SingularMethod) );
            Console.WriteLine( nameof(MyClass1.OverloadedMethod) );

            return meta.Proceed();
        }
    }

    public class MyClass1
    {
        public void SingularMethod() { }

        public void OverloadedMethod() { }

        public void OverloadedMethod( int i ) { }
    }

    internal class TargetCode
    {
        private string Method( MyClass1 a )
        {
            return "";
        }
    }
}