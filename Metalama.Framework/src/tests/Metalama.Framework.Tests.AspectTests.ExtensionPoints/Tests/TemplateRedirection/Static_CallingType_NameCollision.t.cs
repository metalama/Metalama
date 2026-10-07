[Redirect]
internal class Program
{
  public static int Compute_Interceptor = 0;
  public static void TestMain() => Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Static_CallingType_NameCollision.Program.Compute_Interceptor1(21) + Compute_Interceptor);
  private static global::System.Int32 Compute_Interceptor1(global::System.Int32 x)
  {
    var x_1 = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Static_CallingType_NameCollision.Source.Compute(x);
    global::System.Console.WriteLine($"Computed {x_1}.");
    return (global::System.Int32)x_1;
  }
}