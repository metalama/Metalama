[Redirect]
internal class Program
{
  public static void TestMain()
  {
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Async_DefaultTemplate_AnyAwaitable.Program.ComputeAsync_Interceptor(21).Result);
    global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Async_DefaultTemplate_AnyAwaitable.Program.RunAsync_Interceptor().Wait();
  }
  internal static async global::System.Threading.Tasks.Task<global::System.Int32> ComputeAsync_Interceptor(global::System.Int32 x)
  {
    try
    {
      return (await global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Async_DefaultTemplate_AnyAwaitable.Source.ComputeAsync(x));
    }
    finally
    {
      global::System.Console.WriteLine("Done.");
    }
  }
  internal static async global::System.Threading.Tasks.Task RunAsync_Interceptor()
  {
    try
    {
      await global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Async_DefaultTemplate_AnyAwaitable.Source.RunAsync();
      return;
    }
    finally
    {
      global::System.Console.WriteLine("Done.");
    }
  }
}