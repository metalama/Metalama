[Redirect]
internal class Program
{
  public static void TestMain()
  {
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.GenericMethod_RunTimeTypeParameter.Program.Echo_Interceptor<global::System.Int32>(1));
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.GenericMethod_RunTimeTypeParameter.Program.Echo_Interceptor<global::System.String>("a"));
  }
  internal static T Echo_Interceptor<T>(T value)
  {
    global::System.Console.WriteLine($"Echoing a {typeof(T).Name}.");
    return global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.GenericMethod_RunTimeTypeParameter.Source.Echo<T>(value);
  }
}