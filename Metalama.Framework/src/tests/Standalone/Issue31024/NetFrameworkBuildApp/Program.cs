// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using ClassLibrary1;
using Metalama.Framework.Aspects;
using System;

namespace ConsoleApp1
{
    // This aspect is defined directly in the .NET Framework project to force compile-time compilation,
    // which exercises the GetLanguageVersionFromMSBuild() code path in LanguageVersionProvider
    // (i.e., when NETCoreSdkVersion is empty or not available).
    public class LogAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( $"Entering {meta.Target.Method.Name}" );
            return meta.Proceed();
        }
    }

    internal class Program : IInterface
    {
        [Log]
        static void Main( string[] args )
        {
            Console.WriteLine( "Hello, world." );
        }
    }
}
