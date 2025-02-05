namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.AsyncMethod_CrossAssembly;

using System;
using System.Threading.Tasks;

// <target>
[Test]
public class TestClass
{
    public string DoSomething(string text)
    {
        Console.WriteLine("Hello");

        return null!;
    }

    public async Task DoSomethingAsync(string text)
    {
        await Task.Yield();

        Console.WriteLine("Hello");
    }

    public async Task<string> DoSomethingAsyncT(string text)
    {
        await Task.Yield();

        Console.WriteLine("Hello");

        return null!;
    }

    public async void DoSomethingAsyncVoid(string text)
    {
        await Task.Yield();

        Console.WriteLine("Hello");
    }
}