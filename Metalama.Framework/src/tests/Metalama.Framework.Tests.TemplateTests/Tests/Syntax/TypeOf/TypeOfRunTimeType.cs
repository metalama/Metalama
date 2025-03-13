// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable CS8600, CS8603
using System;
using Metalama.Framework.Code;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.TypeOf.TypeOfRunTimeType
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            var rt = meta.RunTime( typeof(MyClass1) );
            var ct = meta.CompileTime( typeof(MyClass1) );
            Console.WriteLine( "rt=" + rt );
            Console.WriteLine( "ct=" + ct );

            if (( (IParameter)meta.Target.Parameters[0] ).Type.IsConvertibleTo( typeof(MyClass1) ))
            {
                Console.WriteLine( "Oops" );
            }

            // Use in a run-time class.
            Console.WriteLine( typeof(MyClass1) );
            Console.WriteLine( typeof(MyClass1).FullName );

            // Use in a compile-time class.
            _ = TypeFactory.GetType( typeof(MyClass1) );

            return meta.Proceed();
        }
    }

    public class MyClass1 { }

    internal class TargetCode
    {
        private string Method( MyClass1 a )
        {
            return "";
        }
    }
}