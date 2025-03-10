// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER)
# endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.AsyncIterator_CrossAssembly;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Program
{
    private static async Task TestMain()
    {
        const string text = "testText";
        var test = new TestClass();

        await foreach (var item in test.AsyncEnumerable(text))
        {
            Console.WriteLine($"{item};");
        }

        var enumerator = test.AsyncEnumerator(text);

        while (await enumerator.MoveNextAsync())
        {
            Console.WriteLine($"{enumerator.Current};");
        }
    }
}

// <target>
[Test]
public class TestClass
{
    public async IAsyncEnumerable<string> AsyncEnumerable( string text )
    {
        await Task.Yield();
        yield return "Hello";
        await Task.Yield();
        yield return text;
    }

    public async IAsyncEnumerator<string> AsyncEnumerator(string text)
    {
        await Task.Yield();
        yield return "Hello";
        await Task.Yield();
        yield return text;
    }
}