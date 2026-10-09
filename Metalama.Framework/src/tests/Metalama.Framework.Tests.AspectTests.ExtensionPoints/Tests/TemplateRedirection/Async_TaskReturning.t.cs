[Redirect]
internal class Program
{
  public static void TestMain() => Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Async_TaskReturning.Program.ComputeAsync_Interceptor(21).Result);
  internal static global::System.Threading.Tasks.Task<global::System.Int32> ComputeAsync_Interceptor(global::System.Int32 x)
  {
    global::System.Console.WriteLine("Starting.");
    return global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Async_TaskReturning.Source.ComputeAsync(x);
  }
}