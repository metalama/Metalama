// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.FileLocalAspect_Nested_NotSupported;

// An aspect class nested in a type declared with the file modifier has a metadata name that carries the prefix of the
// file-local type, so it cannot be resolved either. The build must report the same dedicated error as for a file-local
// aspect class. See issue #2058.

[RunTimeOrCompileTime]
file class Aspects
{
    public class LogAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( $"{meta.Target.Method.Name} started." );

            return meta.Proceed();
        }
    }
}

// <target>
file class Target
{
    [Aspects.Log]
    public int Add( int a, int b ) => a + b;
}
