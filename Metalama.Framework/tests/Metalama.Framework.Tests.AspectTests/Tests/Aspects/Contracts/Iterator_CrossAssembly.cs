namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Iterator_CrossAssembly;

using System;
using System.Collections;
using System.Collections.Generic;

public class Program
{
    private static void TestMain()
    {
        const string text = "testText";
        var test = new TestClass();

        foreach (var item in test.Enumerable(text))
        {
            Console.WriteLine($"{item};");
        }

        var enumerator1 = test.Enumerator(text);

        while (enumerator1.MoveNext())
        {
            Console.WriteLine($"{enumerator1.Current};");
        }

        foreach (var item in test.EnumerableT(text))
        {
            Console.WriteLine($"{item};");
        }

        var enumerator2 = test.EnumeratorT(text);

        while (enumerator2.MoveNext())
        {
            Console.WriteLine($"{enumerator2.Current};");
        }
    }
}

// <target>
[Test]
public class TestClass
{
    public IEnumerable Enumerable(string text)
    {
        yield return "Hello";
        yield return text;
    }

    public IEnumerator Enumerator(string text)
    {
        yield return "Hello";
        yield return text;
    }

    public IEnumerable<string> EnumerableT( string text )
    {
        yield return "Hello";
        yield return text;
    }

    public IEnumerator<string> EnumeratorT(string text)
    {
        yield return "Hello";
        yield return text;
    }
}