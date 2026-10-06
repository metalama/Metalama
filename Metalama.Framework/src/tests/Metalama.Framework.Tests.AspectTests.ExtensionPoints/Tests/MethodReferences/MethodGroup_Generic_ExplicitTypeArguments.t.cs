[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    Func<string?> function = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_Generic_ExplicitTypeArguments.Interceptors.Create<global::System.String>;
    Console.WriteLine(function() == null);
  }
}