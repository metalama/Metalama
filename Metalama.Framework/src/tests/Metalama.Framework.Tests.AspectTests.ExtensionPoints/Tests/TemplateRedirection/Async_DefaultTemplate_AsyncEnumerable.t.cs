[Redirect]
internal class Program
{
  public static async Task TestMain()
  {
    await foreach (var i in global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Async_DefaultTemplate_AsyncEnumerable.Program.GetEnumerable_Interceptor())
    {
      Console.WriteLine($"Received {i}.");
    }
    await using (var enumerator = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Async_DefaultTemplate_AsyncEnumerable.Program.GetEnumerator_Interceptor())
    {
      while (await enumerator.MoveNextAsync())
      {
        Console.WriteLine($"Received {enumerator.Current}.");
      }
    }
  }
  internal static async global::System.Collections.Generic.IAsyncEnumerable<global::System.Int32> GetEnumerable_Interceptor()
  {
    global::System.Console.WriteLine("Before.");
    var result = (await global::Metalama.Framework.RunTime.RunTimeAspectHelper.BufferAsync(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Async_DefaultTemplate_AsyncEnumerable.Source.GetEnumerable()));
    global::System.Console.WriteLine("After.");
    await foreach (var r in result)
    {
      yield return r;
    }
    yield break;
  }
  internal static async global::System.Collections.Generic.IAsyncEnumerator<global::System.Int32> GetEnumerator_Interceptor()
  {
    global::System.Console.WriteLine("Before.");
    var result = (await global::Metalama.Framework.RunTime.RunTimeAspectHelper.BufferAsync(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Async_DefaultTemplate_AsyncEnumerable.Source.GetEnumerator()));
    global::System.Console.WriteLine("After.");
    await using (result)
    {
      while (await result.MoveNextAsync())
      {
        yield return result.Current;
      }
    }
    yield break;
  }
}