[Redirect]
internal class Program
{
  public static void TestMain() => Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Static_IntroducedType.Program.Helpers.Compute_Interceptor(21));
  class Helpers
  {
    internal static global::System.Int32 Compute_Interceptor(global::System.Int32 x)
    {
      var x_1 = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Static_IntroducedType.Source.Compute(x);
      global::System.Console.WriteLine($"Computed {x_1} in Helpers.");
      return (global::System.Int32)x_1;
    }
  }
}