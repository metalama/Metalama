// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.FileLocalAspect_NotSupported;

// An aspect class cannot be declared with the file modifier. The build must report a dedicated error that names the
// aspect class as it is written in source, instead of LAMA0113 with the metadata name of the file-local type.
// See issue #2058.

file class LogAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"{meta.Target.Method.Name} started." );

        return meta.Proceed();
    }
}

// <target>
public class Target
{
    [Log]
    public int Add( int a, int b ) => a + b;
}
