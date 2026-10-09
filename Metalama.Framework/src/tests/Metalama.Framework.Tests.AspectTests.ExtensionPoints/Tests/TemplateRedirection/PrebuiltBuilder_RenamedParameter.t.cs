[Redirect]
internal class Program
{
  public static void TestMain() => Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.PrebuiltBuilder_RenamedParameter.Program.Compute_Interceptor(21));
  internal static global::System.Int32 Compute_Interceptor(global::System.Int32 value)
  {
    global::System.Console.WriteLine($"Computing for {value}.");
    return global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.PrebuiltBuilder_RenamedParameter.Source.Compute(value);
  }
}