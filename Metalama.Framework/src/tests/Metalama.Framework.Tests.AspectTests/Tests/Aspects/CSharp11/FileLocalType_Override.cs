// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.FileLocalType_Override;

// A declaration of a file-local type is identified by a SerializableDeclarationId that carries the metadata name of
// that type, so that two file-local types of the same name declared in two files are told apart. See issue #662.
// The compile-time pipeline never needed that identifier for an override, so this test guards a case that already
// worked. The identifier is what the design-time pipeline requires, and its absence cost the whole project its
// Metalama features in the editor. See issue #2051.

public class LogAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"{meta.Target.Method.Name} started." );

        return meta.Proceed();
    }
}

file class FileLocalTarget
{
    [Log]
    public int Add( int a, int b )
    {
        return a + b;
    }

    [Log]
    public T Echo<T>( T value )
    {
        return value;
    }
}

public class Caller
{
    public int Call() => new FileLocalTarget().Add( 1, 2 );
}
